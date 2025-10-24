using BitTorrentClient.Core.Presentation.UdpTracker.Models;
using BitTorrentClient.Core.Transport.Trackers.Interface;

namespace BitTorrentClient.Core.Transport.Trackers;

public sealed class LazyTrackerFinder : ITrackerFetcher
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
        _innerFetcher?.Dispose();
    }
}