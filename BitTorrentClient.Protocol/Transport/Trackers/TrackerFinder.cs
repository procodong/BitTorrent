using Microsoft.Extensions.Logging;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.Trackers.Exceptions;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public sealed class TrackerFinder : IDisposable
{
    private readonly ITrackerConnector _trackerConnector;
    private readonly ILogger _logger;

    public TrackerFinder(ILogger logger, ITrackerConnector connector)
    {
        _logger = logger;
        _trackerConnector = connector;
    }

    public async Task<ITrackerFetcher> FindTrackerAsync(Uri[][] uris, TrackerUpdate initialUpdate, CancellationToken cancellationToken = default)
    {
        var canceller = new CancellationTokenSource();
        await using var _ = cancellationToken.Register(() => canceller.Cancel());
        var tasks = uris
            .SelectMany(uri => uri.OrderBy(_ => Random.Shared.Next()))
            .Select(uri => _trackerConnector.ConnectAsync(uri, initialUpdate, canceller.Token))
            .ToList();
        
        ITrackerFetcher? fetcher = null;
        for (var readyTasks = 0; readyTasks < tasks.Count; readyTasks++)
        {
            var trackerTask = await Task.WhenAny(tasks);
            try
            {
                var tracker = await trackerTask;
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

            for (var i = 0; i < tasks.Count; i++)
            {
                if (tasks[i] == trackerTask)
                {
                    tasks[i] = Never<ITrackerFetcher>();
                }
            }
        }
        return fetcher ?? throw new NoValidTrackerException();
    }

    private static async Task<T> Never<T>()
    {
        await Task.Delay(-1);
        return default!;
    }

    public void Dispose()
    {
        _trackerConnector.Dispose();
    }
}
