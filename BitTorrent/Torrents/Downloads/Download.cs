
using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Storage;
using BitTorrentClient.Torrents.Peers.Errors;
using BitTorrentClient.Utils;
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace BitTorrentClient.Torrents.Downloads;
public class Download : IDisposable, IAsyncDisposable
{
    private readonly Torrent _torrent;
    private readonly Config _config;
    private readonly ZeroCopyBitArray _downloadedPieces;
    private readonly DataTransferCounter _recentDataTransfer = new();
    private readonly Stopwatch _recentTransferUpdateWatch = Stopwatch.StartNew();
    private readonly DownloadStorage _storage;
    private readonly List<BlockCursor> _pieceRegisters = [];
    private readonly ZeroCopyBitArray _requestedPieces;
    private readonly SlotMap<ChannelWriter<int>> _haveWriters = [];
    private readonly List<int> _rarestPieces = [];
    private int _downloadedPiecesCount = 0;
    private int _requestedPiecesOffset = 0;

    public Download(Torrent torrent, DownloadStorage storage, Config config)
    {
        _torrent = torrent;
        _config = config;
        _downloadedPieces = new(torrent.NumberOfPieces);
        _storage = storage;
        _requestedPieces = new(torrent.NumberOfPieces);
    }

    public ZeroCopyBitArray DownloadedPieces => _downloadedPieces;
    public bool HasDownloadedPieces => _downloadedPiecesCount != 0;
    public Torrent Torrent => _torrent;
    public Config Config => _config;
    public int MaxMessageLength => int.Max(_config.RequestSize + 13, _downloadedPieces.Buffer.Length + 5);
    public bool FinishedDownloading => _downloadedPiecesCount >= _torrent.NumberOfPieces;
    public List<int> RarestPieces => _rarestPieces;
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
    public long UploadRateTarget => FinishedDownloading ? _config.TargetUploadSeeding : Config.TargetUpload;
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

    public double SecondsSinceTimerReset => _recentTransferUpdateWatch.Elapsed.TotalSeconds;

    public int AddPeer(ChannelWriter<int> communicator)
    {
        lock (_haveWriters)
        {
            return _haveWriters.Add(communicator);
        }
    }

    public void RemovePeer(int index)
    {
        lock (_haveWriters)
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
        var files = _storage.GetStream(block.Piece.PieceIndex, block.Begin, block.Length);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(block.Length);
        await stream.ReadExactlyAsync(buffer.AsMemory(..block.Length), cancellationToken);
        await files.WriteAsync(buffer.AsMemory(..block.Length), cancellationToken);
        lock (block.Piece.Hasher)
        {
            block.Piece.Hasher.Hash(buffer, block.Length, block.Begin / Config.RequestSize);
        }
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        Interlocked.Add(ref _recentDataTransfer.Downloaded, block.Length);
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        Interlocked.Increment(ref _downloadedPiecesCount);
        Memory<byte> correctHash = Torrent.Pieces.AsMemory(block.Piece.PieceIndex * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes);
        if (!correctHash.Span.SequenceEqual(block.Piece.Hasher.Finish()))
        {
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        lock (_haveWriters)
        {
            _downloadedPieces[block.Piece.PieceIndex] = true;
            foreach (var peer in _haveWriters)
            {
                if (!peer.TryWrite(block.Piece.PieceIndex))
                {
                    _ = peer.WriteAsync(block.Piece.PieceIndex).AsTask();
                }
            }
        }
    }

    public PieceStream? RequestBlock(PieceRequest request)
    {
        ValidateRequest(request);
        if (UploadRate >= UploadRateTarget) return null;
        return _storage.GetStream(request.Index, request.Begin, request.Length);
    }

    public void Cancel(Block block)
    {
        _pieceRegisters.Add(new(block));
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

    public Block? AssignBlock(BitArray ownedPieces)
    {
        if (DownloadRate > Config.TargetDownload || FinishedDownloading)
        {
            return null;
        }
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
        if (download.Position == download.Piece.Size)
        {
            _requestedPieces[download.Piece.PieceIndex] = true;
            while (_requestedPieces[_requestedPiecesOffset])
            {
                _requestedPiecesOffset++;
            }
        }
        return request;
    }

    private int? SeachPiece(BitArray ownedPieces)
    {
        int index = _pieceRegisters.FindIndex(d => ownedPieces[d.Piece.PieceIndex]);
        if (index != -1) return index;
        var creation = CreateDownload(ownedPieces);
        if (creation is null) return default;
        int i = _pieceRegisters.Count;
        _pieceRegisters.Add(new(new(creation, 0, creation.Size)));
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

    private PieceDownload? FindNextPiece(IEnumerable<int> pieces, BitArray ownedPieces)
    {
        int? piece = pieces.Find(p => CanBeRequested(p, ownedPieces));
        if (piece is null) return default;
        int size = PieceSize(piece.Value);
        return new PieceDownload(size, piece.Value, new(size / Config.RequestSize + 1));
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(Torrent.PieceSize, Torrent.TotalSize - piece * Torrent.PieceSize);
    }

    public void Dispose()
    {
        _storage.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _storage.DisposeAsync();
    }
}
