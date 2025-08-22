using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Protocol.Transport.Trackers;

namespace BitTorrentClient.Application.Launchers.Downloads;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(DownloadState download, DownloadStorage storage, ITrackerFetcher tracker);
}
