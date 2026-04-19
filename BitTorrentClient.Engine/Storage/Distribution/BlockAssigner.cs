using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Storage.Interface;

namespace BitTorrentClient.Engine.Storage.Distribution;
public sealed class BlockAssigner
{
    private readonly int _segmentSize;
    private readonly DownloadData _download;
    private readonly List<BlockCursor> _pieceRegisters = [];
    private readonly ZeroCopyBitArray _requestedPieces;
    private readonly List<int> _rarestPieces = [];
    private readonly IPieceSelectionStrategy _strategy;
    private readonly int[] _pieceBuffer;
    private int _piecePosition;

    public BlockAssigner(DownloadData download, IPieceSelectionStrategy strategy, int segmentSize)
    {
        _strategy = strategy;
        _segmentSize = segmentSize;
        _download = download;
        _requestedPieces = new(download.PieceCount);
        _pieceBuffer = new int[1 << 6];
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
            
        }
        block = request;
        return true;
    }

    public void UpdatePieces(IEnumerable<LazyBitArray> peerPieces)
    {
        _strategy.SelectPieces(_requestedPieces, peerPieces, _pieceBuffer);
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
        return FindNextPiece(ownedPieces);
    }

    private bool CanBeRequested(int pieceIndex, LazyBitArray ownedPieces)
    {
        return !_requestedPieces[pieceIndex] && ownedPieces[pieceIndex] && pieceIndex > _requestedPiecesOffset;
    }

    private PieceDownload? FindNextPiece(LazyBitArray ownedPieces)
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
