using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;

namespace BitTorrentClient.Engine.Launchers.Interface;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker);
}
