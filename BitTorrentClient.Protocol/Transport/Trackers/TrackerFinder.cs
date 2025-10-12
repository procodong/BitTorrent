using Microsoft.Extensions.Logging;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.Trackers.Exceptions;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public sealed class TrackerFinder : IDisposable
{
    private readonly ITrackerConnector _trackerConnector;
    private readonly ILogger _logger;

    public TrackerFinder(ITrackerConnector connector, ILogger logger)
    {
        _trackerConnector = connector;
        _logger = logger;
    }

    public async Task<ITrackerFetcher> FindTrackerAsync(Uri[][] uris, TrackerUpdate initialUpdate, CancellationToken cancellationToken = default)
    {
        var canceller = new CancellationTokenSource();
        await using var _ = cancellationToken.Register(() => canceller.Cancel());
        var trackers = new TaskListener<EventType>();
        int trackerCount = 0;
        foreach (var uri in uris.SelectMany(uri => uri.OrderBy(_ => Random.Shared.Next())))
        {
            trackerCount++;
            trackers.AddTask(EventType.Value, _trackerConnector.ConnectAsync(uri, initialUpdate, canceller.Token));
        }

        ITrackerFetcher? fetcher = null;
        for (var readyTasks = 0; readyTasks < trackerCount; readyTasks++)
        {
            var (_, trackerTask) = await trackers.WaitAsync();
            try
            {
                var tracker = await (Task<ITrackerFetcher>)trackerTask;
                if (fetcher is null)
                {
                    fetcher = tracker;
                    await canceller.CancelAsync();
                }
                else
                {
                    tracker.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (ex is TrackerException)
                {
                    _logger.LogError(ex, "Tracker exception connecting to tracker: {}", ex);
                }
            }
        }
        return fetcher ?? throw new NoValidTrackerException();
    }

    public void Dispose()
    {
        _trackerConnector.Dispose();
    }
    
    enum EventType {Value}
}