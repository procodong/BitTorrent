using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;

namespace BitTorrentClient.Engine.Launchers.Interface;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker);
}
