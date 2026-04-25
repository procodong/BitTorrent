using BitTorrentClient.Engine.Storage.Interface;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Distribution;

public class SynchronizedBlockAssigner : IBlockAssigner
{
    private readonly BlockAssigner _downloader;
    private readonly Lock _lock;

    public SynchronizedBlockAssigner(BlockAssigner downloader)
    {
        _downloader = downloader;
        _lock = new();
    }

    public int RemainingSuppliedPieces
    {
        get
        {
            using (_lock.EnterScope())
            {
                return _downloader.RemainingSuppliedPieces;
            }
        }
    }


    public void Cancel(Block block)
    {
        using (_lock.EnterScope())
        {
            _downloader.Cancel(block);
        }
    }

    public void SupplyPieces(Func<Span<int>, ZeroCopyBitArray, int> action)
    {
        using (_lock.EnterScope())
        {
            _downloader.SupplyPieces(action);
        }
    }

    public bool TryAssignBlock(LazyBitArray ownedPieces, out Block block)
    {
        using (_lock.EnterScope())
        {
            return _downloader.TryAssignBlock(ownedPieces, out block);
        }
    }
}