
using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Helpers;
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Downloads;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;
public class Downloader
{
    private readonly DownloadState _state;
    private readonly List<BlockCursor> _pieceRegisters = [];
    private readonly ZeroCopyBitArray _requestedPieces;
    private readonly SlotMap<ChannelWriter<int>> _haveWriters = [];
    private readonly List<int> _rarestPieces = [];
    private int _downloadedPiecesCount;
    private int _requestedPiecesOffset;

    public Downloader(DownloadState state)
    {
        _state = state;
        _requestedPieces = new(state.Download.Torrent.NumberOfPieces);
    }

    public LazyBitArray DownloadedPieces => _state.DownloadedPieces;
    public Torrent Torrent => _state.Download.Torrent;
    public Config Config => _state.Download.Config;
    public bool FinishedDownloading => _downloadedPiecesCount >= _state.Download.Torrent.NumberOfPieces;
    
    public async Task SaveBlockAsync(Stream stream, Block block, CancellationToken cancellationToken = default)
    {
        await stream.ReadExactlyAsync(block.Piece.Buffer.AsMemory(block.Begin, block.Length), cancellationToken);
        int newDownloaded = Interlocked.Add(ref block.Piece.Downloaded, block.Length);
        _state.RecentDataTransfer.AtomicAddDownload(block.Length);
        if (newDownloaded < block.Piece.Size)
        {
            return;
        }
        Interlocked.Increment(ref _downloadedPiecesCount);
        var correctHash = Torrent.Pieces.AsMemory(block.Piece.PieceIndex * SHA1.HashSizeInBytes, SHA1.HashSizeInBytes);
        var hash = SHA1.HashData(block.Piece.Buffer);
        if (!hash.AsSpan().SequenceEqual(correctHash.Span))
        {
            Cancel(block.Piece);
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        var files = _state.Storage.GetStream(block.Piece.PieceIndex, block.Begin, block.Length);
        await files.WriteAsync(block.Piece.Buffer.AsMemory(..block.Piece.Size), cancellationToken);
        ArrayPool<byte>.Shared.Return(block.Piece.Buffer);
        lock (_haveWriters)
        {
            _state.DownloadedPieces[block.Piece.PieceIndex] = true;
            foreach (var peer in _haveWriters)
            {
                if (!peer.TryWrite(block.Piece.PieceIndex))
                {
                    _ = peer.WriteAsync(block.Piece.PieceIndex).AsTask();
                }
            }
        }
    }

    public PieceStream RequestBlock(PieceRequest request)
    {
        ValidateRequest(request);
        return _state.Storage.GetStream(request.Index, request.Begin, request.Length);
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

    public bool TryAssignBlock(LazyBitArray ownedPieces, out Block block)
    {
        if (_state.TransferRate.Download > Config.TargetDownload || FinishedDownloading)
        {
            block = default;
            return false;
        }
        var slot = SearchPiece(ownedPieces);
        if (!slot.HasValue)
        {
            block = default;
            return false;
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
        block = request;
        return true;
    }

    private int? SearchPiece(LazyBitArray ownedPieces)
    {
        int index = _pieceRegisters.FindIndex(d => ownedPieces[d.Piece.PieceIndex]);
        if (index != -1) return index;
        var creation = CreateDownload(ownedPieces);
        if (creation is null) return default;
        int i = _pieceRegisters.Count;
        _pieceRegisters.Add(new(creation));
        return i;
    }

    private PieceDownload? CreateDownload(LazyBitArray ownedPieces)
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
        return FindNextPiece(Enumerable.Range(_requestedPiecesOffset, Torrent.NumberOfPieces - _requestedPiecesOffset), ownedPieces);
    }

    private bool CanBeRequested(int pieceIndex, LazyBitArray ownedPieces)
    {
        return !_requestedPieces[pieceIndex] && ownedPieces[pieceIndex] && pieceIndex > _requestedPiecesOffset;
    }

    private PieceDownload? FindNextPiece(IEnumerable<int> pieces, LazyBitArray ownedPieces)
    {
        int? piece = pieces.Find(p => CanBeRequested(p, ownedPieces));
        if (piece is null) return default;
        int size = PieceSize(piece.Value);
        var buffer = ArrayPool<byte>.Shared.Rent(size);
        return new(size, piece.Value, buffer);
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(Torrent.PieceSize, Torrent.TotalSize - piece * Torrent.PieceSize);
    }
}
