using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Engine.Storage.Distribution;
public sealed class BlockAssigner
{
    private readonly int _segmentSize;
    private readonly DownloadData _download;
    private readonly List<BlockCursor> _pieceRegisters = [];
    private readonly ZeroCopyBitArray _requestedPieces;
    private readonly List<int> _rarestPieces = [];
    private int _requestedPiecesOffset;

    public BlockAssigner(DownloadData download, int segmentSize)
    {
        _segmentSize = segmentSize;
        _download = download;
        _requestedPieces = new(download.PieceCount);
    }

    public void Cancel(Block block)
    {
        _pieceRegisters.Add(new(block));
    }

    public bool TryAssignBlock(LazyBitArray ownedPieces, out Block block)
    {
        var slot = SearchPiece(ownedPieces);
        if (!slot.HasValue)
        {
            block = default;
            return false;
        }
        var index = slot.Value;
        var download = _pieceRegisters[index];
        var request = download.GetRequest(_segmentSize);
        if (download.Remaining == 0)
        {
            _pieceRegisters.SwapRemove(index);
        }
        if (download.Position == download.Piece.Size)
        {
            _requestedPieces[download.Piece.Index] = true;
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
        var index = _pieceRegisters.FindIndex(d => ownedPieces[d.Piece.Index]);
        if (index != -1) return index;
        var creation = CreateDownload(ownedPieces);
        if (creation is null) return default;
        var i = _pieceRegisters.Count;
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
                _rarestPieces.Remove(rarePiece.Index);
                return rarePiece;
            }
        }
        return FindNextPiece(Enumerable.Range(_requestedPiecesOffset, _download.PieceCount - _requestedPiecesOffset), ownedPieces);
    }

    private bool CanBeRequested(int pieceIndex, LazyBitArray ownedPieces)
    {
        return !_requestedPieces[pieceIndex] && ownedPieces[pieceIndex] && pieceIndex > _requestedPiecesOffset;
    }

    private PieceDownload? FindNextPiece(IEnumerable<int> pieces, LazyBitArray ownedPieces)
    {
        var piece = pieces.Find(p => CanBeRequested(p, ownedPieces));
        if (piece is null) return default;
        var size = PieceSize(piece.Value);
        var hasher = new PieceHasher(_segmentSize, size.DivWithRemainder(_segmentSize));
        return new(size, piece.Value, hasher);
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(_download.PieceSize, _download.Size - piece * _download.PieceSize);
    }
}
