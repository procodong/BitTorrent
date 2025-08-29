using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Protocol.Transport.Trackers.Interface;
public interface ITrackerFetcher : IDisposable
{
    Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default);
}
