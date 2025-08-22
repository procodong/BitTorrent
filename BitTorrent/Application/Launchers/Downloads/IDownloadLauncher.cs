using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Transport.Trackers;

namespace BitTorrentClient.Application.Launchers.Downloads;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker);
}
