using Microsoft.Extensions.Logging;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.Trackers.Exceptions;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public class TrackerFinder
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
        var readyTasks = new HashSet<Task>(tasks.Count);
        while (tasks.Count != readyTasks.Count)
        {
            var trackerTask = await Task.WhenAny(tasks.Where(t => !readyTasks.Contains(t)));
            try
            {
                var tracker = await trackerTask;
                if (fetcher is null)
                {
                    fetcher = tracker;
                    await canceller.CancelAsync();
                }
                else if (fetcher is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (ex is TrackerException)
                {
                    _logger.LogError(ex, "Tracker exception connecting to tracker: {}", ex);
                }
            }
            readyTasks.Add(trackerTask);
        }
        return fetcher ?? throw new NoValidTrackerException();
    }
}
