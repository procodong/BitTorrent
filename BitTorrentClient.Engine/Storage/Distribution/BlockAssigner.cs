using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Storage.Interface;

namespace BitTorrentClient.Engine.Storage.Distribution;
public sealed class BlockAssigner : IBlockAssigner
{
    private readonly int _segmentSize;
    private readonly DownloadData _download;
    private readonly List<BlockCursor> _pieceRegisters = [];
    private readonly ZeroCopyBitArray _requestedPieces;
    private readonly PiecesCursor _cursor;



    public BlockAssigner(DownloadData download, int segmentSize, PiecesCursor cursor)
    {
        _segmentSize = segmentSize;
        _download = download;
        _requestedPieces = new(download.PieceCount);
        _cursor = cursor;
    }

    public void Cancel(Block block)
    {
        _pieceRegisters.Add(new(block));
    }

    public void SupplyPieces(Func<Span<int>, ZeroCopyBitArray, int> action)
    {
        _cursor.SupplyPieces(buf => action(buf, _requestedPieces));
    }
    
    public int RemainingSuppliedPieces => _cursor.Remaining;

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
        block = request;
        return true;
    }

    private int? SearchPiece(LazyBitArray ownedPieces)
    {
        var index = _pieceRegisters.FindIndex(d => ownedPieces[d.Piece.Index]);
        if (index != -1) return index;
        if (!_cursor.TryGetNext(ownedPieces, out int piece)) return default;
        _requestedPieces[piece] = true;
        var size = PieceSize(piece);
        var hasher = new PieceHasher(_segmentSize, size.DivWithRemainder(_segmentSize));
        var download = new PieceDownload(size, piece, hasher);
        var i = _pieceRegisters.Count;
        _pieceRegisters.Add(new(download));
        return i;
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(_download.PieceSize, _download.Size - piece * _download.PieceSize);
    }
}
