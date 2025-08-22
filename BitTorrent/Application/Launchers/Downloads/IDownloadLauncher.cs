using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Protocol.Networking.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Launchers.Downloads;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(DownloadState download, ITrackerFetcher tracker);
}
