
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
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Downloads;
public class Download(Torrent torrent, DownloadSaveManager files, Config config) : IDisposable, IAsyncDisposable
{
    public readonly Torrent Torrent = torrent;
    public readonly Config Config = config;
    public readonly BitArray DownloadedPieces = new(torrent.NumberOfPieces);
    public readonly long MaxMessageLength = int.Max(config.RequestSize + 13, torrent.NumberOfPieces + 6);
    private readonly DataTransferCounter _statistics = new();
    private readonly DownloadSaveManager _files = files;
    private readonly List<PieceDownload> _pieceRegisters = [];
    private readonly BitArray _requestedPieces = new(torrent.NumberOfPieces);
    private readonly SlotMap<ChannelWriter<int>> _haveWriters = [];
    private readonly object _haveWritersLock = new();
    private List<int> _rarestPieces = [];
    private int _downloadedPieces = 0;
    private int _downloadedPiecesOffset = 0;

    public bool FinishedDownloading => _downloadedPieces >= Torrent.NumberOfPieces;
    public List<int> RarestPieces
    {
        get => _rarestPieces;
        set => _rarestPieces = value;
    }
    public int DownloadedPiecesCount => _downloadedPieces;
    public DataTransferVector DataTransfered => new(
        Interlocked.Read(ref _statistics.Downloaded), 
        Interlocked.Read(ref _statistics.Uploaded)
        );

    public void AddPeer(ChannelWriter<int> communicator)
    {
        lock (_haveWritersLock)
        {
            _haveWriters.Add(communicator);
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
        Interlocked.Add(ref _statistics.Uploaded, upload);
    }


    public async Task SaveBlockAsync(Stream stream, QueuedPieceRequest piece)
    {
        var files = _files.GetStream(piece.Download.PieceIndex, piece.Request.Begin, piece.Request.Length);
        byte[] buffer = new byte[piece.Request.Length];
        await stream.ReadExactlyAsync(buffer);
        await files.WriteAsync(buffer);
        lock (piece.Download.Hasher)
        {
            piece.Download.Hasher.Hash(buffer, piece.Download.PieceIndex);
        }

        int newDownloaded = Interlocked.Add(ref piece.Download.Downloaded, piece.Request.Length);
        Interlocked.Add(ref _statistics.Downloaded, piece.Request.Length);
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

    public PieceStream RequestBlock(PieceRequest request)
    {
        ValidateRequest(request);
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
        if (download.Downloading == download.Size)
        {
            if (download.Size == PieceSize(download.PieceIndex)) 
            {
                _requestedPieces[download.PieceIndex] = true;
                while (_requestedPieces[_downloadedPiecesOffset])
                {
                    _downloadedPiecesOffset++;
                }
            }
            throw new PieceDownloadFullException();
        }
        return new(download.PieceIndex, downloadOffset, downloadSize);
    }

    public QueuedPieceRequest? AssignBlockRequest(BitArray ownedPieces)
    {
        var slot = SeachPiece(ownedPieces);
        if (slot.HasValue)
        {
            int index = slot.Value;
            try
            {
                var download = _pieceRegisters[index];
                var request = AllocateDownload(_pieceRegisters[index]);
                return new(download, request);
            }
            catch (PieceDownloadFullException)
            {
                _pieceRegisters.SwapRemove(index);
            }
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
        _pieceRegisters.Add(creation);
        return _pieceRegisters.Count - 1;
    }

    private PieceDownload? CreateDownload(BitArray ownedPieces)
    {
        if (_rarestPieces.Count != 0)
        {
            var rarePiece = FindNextPiece(_rarestPieces, ownedPieces);
            if (rarePiece is not null)
            {
                _rarestPieces.Remove(rarePiece.Value.Index);
                return rarePiece.Value.Download;
            }
        }
        var nextPiece = FindNextPiece(Enumerable.Range(_downloadedPiecesOffset, Torrent.NumberOfPieces - _downloadedPiecesOffset), ownedPieces);
        if (nextPiece is not null)
        {
            return nextPiece.Value.Download;
        }
        return default;
    }

    private bool CanBeRequested(int pieceIndex, BitArray ownedPieces)
    {
        return !_requestedPieces[pieceIndex] && ownedPieces[pieceIndex];
    }

    private (int Index, PieceDownload Download)? FindNextPiece(IEnumerable<int> downloads, BitArray ownedPieces)
    {
        return downloads
            .Where(piece => CanBeRequested(piece, ownedPieces))
            .Select((piece, index) => (index, new PieceDownload(PieceSize(piece), piece, new(Config.RequestSize))))
            .First();
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
