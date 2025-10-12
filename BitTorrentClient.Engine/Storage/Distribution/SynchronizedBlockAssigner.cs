using BitTorrentClient.Engine.Models;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Distribution;

public class SynchronizedBlockAssigner
{
    private readonly BlockAssigner _downloader;
    private readonly Lock _lock;

    public SynchronizedBlockAssigner(BlockAssigner downloader)
    {
        _downloader = downloader;
        _lock = new();
    }

    public void Cancel(Block block)
    {
        using (_lock.EnterScope())
        {
            _downloader.Cancel(block);
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