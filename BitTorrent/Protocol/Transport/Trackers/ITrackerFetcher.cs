using BitTorrentClient.Models.Trackers;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public interface ITrackerFetcher
{
    Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default);
}
