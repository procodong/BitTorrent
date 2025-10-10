using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Presentation.Torrent;

namespace BitTorrentClient.Engine.Storage.Distribution;
public sealed class Downloader
{
    private readonly DownloadState _state;
    private readonly List<BlockCursor> _pieceRegisters = [];
    private readonly ZeroCopyBitArray _requestedPieces;
    private readonly List<int> _rarestPieces = [];
    private int _requestedPiecesOffset;

    public Downloader(DownloadState state)
    {
        _state = state;
        _requestedPieces = new(state.Download.Data.PieceCount);
    }

    public LazyBitArray DownloadedPieces => _state.DownloadedPieces;
    public DownloadData Torrent => _state.Download.Data;
    public Config Config => _state.Download.Config;

    public void RegisterDownloaded(long download)
    {
        _state.DataTransfer.AtomicAddDownload(download);
    }

    public void Cancel(Block block)
    {
        _pieceRegisters.Add(new(block));
    }

    public bool TryAssignBlock(LazyBitArray ownedPieces, out Block block)
    {
        if (_state.TransferRate.Download > Config.TargetDownload)
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
        var index = slot.Value;
        var download = _pieceRegisters[index];
        var request = download.GetRequest(Config.PieceSegmentSize);
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
        return FindNextPiece(Enumerable.Range(_requestedPiecesOffset, Torrent.PieceCount - _requestedPiecesOffset), ownedPieces);
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
        var bufferSize = _state.Download.Config.RequestSize * 4;
        var hasher = new PieceHasher(bufferSize, size.DivWithRemainder(bufferSize));
        return new(size, piece.Value, hasher);
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(Torrent.PieceSize, Torrent.Size - piece * Torrent.PieceSize);
    }
}
