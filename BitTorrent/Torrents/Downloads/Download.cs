
using BencodeNET.Torrents;
using BitTorrent.Errors;
using BitTorrent.Files.DownloadFiles;
using BitTorrent.Models.Application;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.PieceSaver.DownloadFiles;
using BitTorrent.Torrents.Downloads.Errors;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Peers.Errors;
using BitTorrent.Utils;
using System.Collections;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Downloads;
public class Download(Torrent torrent, DownloadSaveManager files, Config config) : IDisposable, IAsyncDisposable
{
    public readonly Torrent Torrent = torrent;
    public readonly Config Config = config;
    public readonly BitArray DownloadedPieces = new(torrent.NumberOfPieces);
    public readonly long MaxMessageLength = int.Max(config.RequestSize + 13, torrent.NumberOfPieces + 6);
    private readonly DataTransferCounter _recentDataTransfer = new();
    private readonly Stopwatch _recentTransferUpdateWatch = Stopwatch.StartNew();
    private readonly DownloadSaveManager _files = files;
    private readonly List<PieceDownload> _pieceRegisters = [];
    private readonly BitArray _requestedPieces = new(torrent.NumberOfPieces);
    private readonly SlotMap<ChannelWriter<int>> _haveWriters = [];
    private readonly object _haveWritersLock = new();
    private List<int> _rarestPieces = [];
    private int _downloadedPieces = 0;
    private int _requestedPiecesOffset = 0;

    public bool FinishedDownloading => _downloadedPieces >= Torrent.NumberOfPieces;
    public List<int> RarestPieces
    {
        get => _rarestPieces;
        set => _rarestPieces = value;
    }
    public int DownloadedPiecesCount => _downloadedPieces;
    public DataTransferVector RecentlyTransfered => _recentDataTransfer.Fetch();
    public long UploadRate => (long)(Interlocked.Read(ref _recentDataTransfer.Uploaded) / _recentTransferUpdateWatch.Elapsed.TotalSeconds);
    public long UploadRateTarget => FinishedDownloading ? Config.TargetUpload : Config.TargetUploadSeeding;
    public long DownloadRate => (long)(Interlocked.Read(ref _recentDataTransfer.Downloaded) / _recentTransferUpdateWatch.Elapsed.TotalSeconds);
    public long DownloadRateTarget => FinishedDownloading ? Config.TargetDownload : 0;
    public double SecondsSinceTimerReset => _recentTransferUpdateWatch.Elapsed.TotalSeconds;

    public int AddPeer(ChannelWriter<int> communicator)
    {
        lock (_haveWritersLock)
        {
            return _haveWriters.Add(communicator);
        }
    }

    public void RemovePeer(int index)
    {
        lock (_haveWritersLock)
        {
            _haveWriters.Remove(index);
        }
    }

    public void FinishUpload(long upload)
    {
        Interlocked.Add(ref _recentDataTransfer.Uploaded, upload);
    }

    public DataTransferVector ResetRecentTransfer()
    {
        _recentTransferUpdateWatch.Restart();
        return _recentDataTransfer.FetchReplace(new());
    }

    public async Task SaveBlockAsync(Stream stream, QueuedPieceRequest piece)
    {
        var files = _files.GetStream(piece.Download.PieceIndex, piece.Request.Begin, piece.Request.Length);
        byte[] buffer = new byte[piece.Request.Length];
        await stream.ReadExactlyAsync(buffer);
        await files.WriteAsync(buffer);
        lock (piece.Download.Hasher)
        {
            piece.Download.Hasher.Hash(buffer, piece.Request.Begin);
        }

        int newDownloaded = Interlocked.Add(ref piece.Download.Downloaded, piece.Request.Length);
        Interlocked.Add(ref _recentDataTransfer.Downloaded, piece.Request.Length);
        if (newDownloaded < piece.Download.Size || piece.Download.Size != PieceSize(piece.Download.PieceIndex))
        {
            return;
        }
        Interlocked.Increment(ref _downloadedPieces);
        Memory<byte> correctHash = Torrent.Pieces.AsMemory(piece.Download.PieceIndex * 20, 20);
        IEnumerable<byte> correctHashIter = MemoryMarshal.ToEnumerable<byte>(correctHash);
        if (!piece.Download.Hasher.Finish().SequenceEqual(correctHashIter))
        {
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        DownloadedPieces[piece.Download.PieceIndex] = true;
        lock (_haveWritersLock)
        {
            foreach (var peer in _haveWriters)
            {
                if (peer is null) continue;
                peer.TryWrite(piece.Download.PieceIndex);
            }
        }
    }

    public PieceStream? RequestBlock(PieceRequest request)
    {
        ValidateRequest(request);
        if (UploadRate >= UploadRateTarget) return null;
        return _files.GetStream(request.Index, request.Begin, request.Length);
    }

    public void Cancel(PieceRequest cancel)
    {
        ValidateRequest(cancel);
        var queued = new PieceDownload(cancel.Length, cancel.Index, new(Config.RequestSize), cancel.Begin);
        _pieceRegisters.Add(queued);
    }

    private void ValidateRequest(PieceRequest request)
    {
        int size = PieceSize(request.Index);
        int end = request.Begin + request.Length;
        if (end > size || end < 0)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
    }

    public PieceRequest AllocateDownload(PieceDownload download)
    {
        int downloadSize = int.Min(download.Size - download.Downloading, Config.RequestSize);
        int downloadOffset = download.Downloading;
        download.Downloading += downloadSize;
        if (download.Downloading == PieceSize(download.PieceIndex))
        {
            throw new PieceDownloadFullException();
        }
        if (download.Downloading == download.Size)
        {
            _requestedPieces[download.PieceIndex] = true;
            while (_requestedPieces[_requestedPiecesOffset])
            {
                _requestedPiecesOffset++;
            }
        }
        return new(download.PieceIndex, downloadOffset, downloadSize);
    }

    public QueuedPieceRequest? AssignBlockRequest(BitArray ownedPieces)
    {
        if (DownloadRate > DownloadRateTarget) return null;
        var slot = SeachPiece(ownedPieces);
        if (!slot.HasValue)
        {
            return null;
        }
        int index = slot.Value;
        try
        {
            var download = _pieceRegisters[index];
            var request = AllocateDownload(download);
            return new(download, request);
        }
        catch (PieceDownloadFullException)
        {
            _pieceRegisters.SwapRemove(index);
        }
        return null;
    }

    private int? SeachPiece(BitArray ownedPieces)
    {
        foreach (var (index, pieceDownload) in _pieceRegisters.Indexed())
        {
            if (pieceDownload.Size > pieceDownload.Downloading && ownedPieces[pieceDownload.PieceIndex])
            {
                return index;
            }
        }
        var creation = CreateDownload(ownedPieces);
        if (creation is null) return default;
        var i = _pieceRegisters.Count;
        _pieceRegisters.Add(creation);
        return i;
    }

    private PieceDownload? CreateDownload(BitArray ownedPieces)
    {
        if (_rarestPieces.Count != 0)
        {
            var rarePiece = FindNextPiece(_rarestPieces, ownedPieces);
            if (rarePiece is not null)
            {
                _rarestPieces.Remove(rarePiece.PieceIndex);
                return rarePiece;
            }
        }
        var nextPiece = FindNextPiece(Enumerable.Range(_requestedPiecesOffset, Torrent.NumberOfPieces - _requestedPiecesOffset), ownedPieces);
        if (nextPiece is not null)
        {
            return nextPiece;
        }
        return default;
    }

    private bool CanBeRequested(int pieceIndex, BitArray ownedPieces)
    {
        return !_requestedPieces[pieceIndex] && ownedPieces[pieceIndex] && pieceIndex >= _requestedPiecesOffset;
    }

    private PieceDownload? FindNextPiece(IEnumerable<int> downloads, BitArray ownedPieces)
    {
        int? piece = downloads.Find(piece => CanBeRequested(piece, ownedPieces));
        if (!piece.HasValue) return null;
        return new PieceDownload(PieceSize(piece.Value), piece.Value, new(Config.RequestSize));
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(Torrent.PieceSize, Torrent.TotalSize - piece * Torrent.PieceSize);
    }

    public void Dispose()
    {
        _files.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _files.DisposeAsync();
    }
}
