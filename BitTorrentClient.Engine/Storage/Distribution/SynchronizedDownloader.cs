using BitTorrentClient.Engine.Models;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Distribution;

public class SynchronizedDownloader
{
    private readonly Downloader _downloader;
    private readonly Lock _lock;

    public SynchronizedDownloader(Downloader downloader)
    {
        _downloader = downloader;
        _lock = new();
    }
    
    public LazyBitArray DownloadedPieces => _downloader.DownloadedPieces;
    public Config Config => _downloader.Config;
    public int PieceSize => _downloader.Torrent.PieceSize;
    
    public void RegisterDownloaded(long download)
    {
        _downloader.RegisterDownloaded(download);
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