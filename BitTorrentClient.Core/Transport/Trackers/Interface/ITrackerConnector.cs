using BitTorrentClient.Core.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Core.Transport.Trackers.Interface;

public interface ITrackerConnector : IDisposable
{
    Task<ITrackerFetcher> ConnectAsync(Uri uri, TrackerUpdate initialUpdate, CancellationToken cancellationToken = default);
}