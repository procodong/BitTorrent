using BitTorrentClient.Engine.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Interface;

public interface IBlockAssigner
{
    bool TryAssignBlock(LazyBitArray ownedPieces, out Block block);
    void Cancel(Block block);
    public void SupplyPieces(Func<Span<int>, ZeroCopyBitArray, int> action);
}