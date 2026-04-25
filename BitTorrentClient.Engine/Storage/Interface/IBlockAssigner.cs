using BitTorrentClient.Engine.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Interface;

public interface IBlockAssigner
{
    bool TryAssignBlock(LazyBitArray ownedPieces, out Block block);
    void Cancel(Block block);
    void SupplyPieces(Func<Span<int>, ZeroCopyBitArray, int> action);
    int RemainingSuppliedPieces { get; }
}