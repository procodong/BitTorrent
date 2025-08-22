
using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Downloads;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;
internal class Downloader
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
        int index = slot.Value;
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
        int index = _pieceRegisters.FindIndex(d => ownedPieces[d.Piece.Index]);
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
        int? piece = pieces.Find(p => CanBeRequested(p, ownedPieces));
        if (piece is null) return default;
        int size = PieceSize(piece.Value);
        int blockSize = _state.Download.Config.RequestSize;
        var hasher = new PieceHasher(blockSize, 4, size / blockSize);
        return new(size, piece.Value, hasher);
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(Torrent.PieceSize, Torrent.Size - piece * Torrent.PieceSize);
    }
}
