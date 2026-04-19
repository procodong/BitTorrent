using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Interface;

public interface IPieceSelectionStrategy
{
    int SelectPieces(ZeroCopyBitArray requestedPieces, IEnumerable<LazyBitArray> peerPieces, Span<int> buffer);
}