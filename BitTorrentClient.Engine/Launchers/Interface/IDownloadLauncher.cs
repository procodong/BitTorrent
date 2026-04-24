using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Core.Transport.Trackers.Interface;
using BitTorrentClient.Engine.Models.Config;
using BitTorrentClient.Engine.Storage.Interface;

namespace BitTorrentClient.Engine.Launchers.Interface;
public interface IDownloadLauncher
{
    PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker, NetworkingConfig config, IPieceSelectionStrategy pieceSelectionStrategy);
}
