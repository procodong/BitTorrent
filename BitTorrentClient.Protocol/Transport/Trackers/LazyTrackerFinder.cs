using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;

namespace BitTorrentClient.Protocol.Transport.Trackers;

public class LazyTrackerFinder : ITrackerFetcher
{
    private readonly Uri[][] _trackerUris;
    private readonly TrackerFinder _trackerFinder;
    private ITrackerFetcher? _innerFetcher;

    public LazyTrackerFinder(TrackerFinder finder, Uri[][] trackerUris)
    {
        _trackerFinder = finder;
        _trackerUris = trackerUris;
    }

    public async Task<TrackerResponse> FetchAsync(TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        _innerFetcher ??= await _trackerFinder.FindTrackerAsync(_trackerUris, update, cancellationToken);
        return await _innerFetcher.FetchAsync(update, cancellationToken);
    }
    
    public void Dispose()
    {
        if (_innerFetcher is not null)
        {
            _innerFetcher.Dispose();
        }
    }
}