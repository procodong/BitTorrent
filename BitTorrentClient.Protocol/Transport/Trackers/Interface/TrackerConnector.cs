using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Protocol.Transport.Trackers.Interface;

public interface ITrackerConnector
{
    Task<ITrackerFetcher> ConnectAsync(Uri uri, TrackerUpdate initialUpdate, CancellationToken cancellationToken = default);
}