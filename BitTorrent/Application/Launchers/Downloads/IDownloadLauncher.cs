using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Protocol.Transport.Trackers;

namespace BitTorrentClient.Application.Launchers.Downloads;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(DownloadState download, ITrackerFetcher tracker);
}
