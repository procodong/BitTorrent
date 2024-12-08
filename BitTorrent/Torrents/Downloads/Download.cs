
using BencodeNET.Torrents;
using BitTorrent.Files.DownloadFiles;
using BitTorrent.Models.Application;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.PieceSaver;
using BitTorrent.PieceSaver.DownloadFiles;
using BitTorrent.Torrents.Peers.Errors;
using BitTorrent.Utils;
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
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
    private readonly List<PieceSegmentHandle> _pieceRegisters = [];
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
    public long UploadRate
    {
        get
        {
            lock (_recentTransferUpdateWatch)
            {
                return (long)(Interlocked.Read(ref _recentDataTransfer.Uploaded) / _recentTransferUpdateWatch.Elapsed.TotalSeconds);
            }
        }
    }
    public long UploadRateTarget => FinishedDownloading ? Config.TargetUpload : Config.TargetUploadSeeding;
    public long DownloadRate
    {
        get
        {
            lock (_recentTransferUpdateWatch)
            {
                return (long)(Interlocked.Read(ref _recentDataTransfer.Downloaded) / _recentTransferUpdateWatch.Elapsed.TotalSeconds);
            }
        }
    }
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
        lock (_recentTransferUpdateWatch)
        {
            _recentTransferUpdateWatch.Restart();
        }
        return _recentDataTransfer.FetchReplace(new());
    }

    public async Task SaveBlockAsync(Stream stream, Block block, CancellationToken cancellationToken = default)
    {
        var files = _files.GetStream(block.Piece.PieceIndex, block.Begin, block.Length);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(block.Length);
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        await files.WriteAsync(buffer, cancellationToken);
        lock (block.Piece.Hasher)
        {
            block.Piece.Hasher.Hash(buffer, block.Begin);
        }
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        Interlocked.Add(ref _recentDataTransfer.Downloaded, block.Length);
        if (newDownloaded < block.Piece.Size || block.Piece.Size != PieceSize(block.Piece.PieceIndex))
        {
            return;
        }
        Interlocked.Increment(ref _downloadedPieces);
        Memory<byte> correctHash = Torrent.Pieces.AsMemory(block.Piece.PieceIndex * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes);
        IEnumerable<byte> correctHashIter = MemoryMarshal.ToEnumerable<byte>(correctHash);
        if (!block.Piece.Hasher.Finish().SequenceEqual(correctHashIter))
        {
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        DownloadedPieces[block.Piece.PieceIndex] = true;
        lock (_haveWritersLock)
        {
            foreach (var peer in _haveWriters)
            {
                peer.TryWrite(block.Piece.PieceIndex);
            }
        }
    }

    public PieceStream? RequestBlock(PieceRequest request)
    {
        ValidateRequest(request);
        if (UploadRate >= UploadRateTarget) return null;
        return _files.GetStream(request.Index, request.Begin, request.Length);
    }

    public void Cancel(Block block)
    {
        _pieceRegisters.Add(new(block.Piece, block.Begin, block.Length));
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

    public PieceSegmentHandle? AssignSegment(BitArray ownedPieces)
    {
        if (DownloadRate > DownloadRateTarget) return null;
        var slot = SeachPiece(ownedPieces);
        if (!slot.HasValue)
        {
            return null;
        }
        int index = slot.Value;
        var download = _pieceRegisters[index];
        var request = download.GetRequest(Config.PieceSegmentSize);
        if (download.Remaining == 0)
        {
            _pieceRegisters.SwapRemove(index);
        }
        if (download.Position == PieceSize(download.Piece.PieceIndex))
        {
            _requestedPieces[download.Piece.PieceIndex] = true;
            while (_requestedPieces[_requestedPiecesOffset])
            {
                _requestedPiecesOffset++;
            }
        }
        return new(download.Piece, request.Begin, request.Length);
    }

    private int? SeachPiece(BitArray ownedPieces)
    {
        foreach (var (index, pieceDownload) in _pieceRegisters.Indexed())
        {
            if (ownedPieces[pieceDownload.Piece.PieceIndex])
            {
                return index;
            }
        }
        var creation = CreateDownload(ownedPieces);
        if (creation is null) return default;
        var i = _pieceRegisters.Count;
        _pieceRegisters.Add(new(creation, 0, creation.Size));
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
