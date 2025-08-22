using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public interface ITrackerFetcher
{
    Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default);
}
