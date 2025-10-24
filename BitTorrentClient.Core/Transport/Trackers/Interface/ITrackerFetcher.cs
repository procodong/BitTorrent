using BitTorrentClient.Core.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Core.Transport.Trackers.Interface;
public interface ITrackerFetcher : IDisposable
{
    Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default);
}
