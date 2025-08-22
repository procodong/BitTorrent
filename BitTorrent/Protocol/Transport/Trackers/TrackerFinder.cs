using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using BitTorrentClient.Protocol.Transport.Trackers.Exceptions;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public class TrackerFinder : ITrackerFinder
{
    private readonly int _port;
    private readonly int _peerBufferSize;
    private readonly Random _random;
    private readonly ILogger _logger;

    public TrackerFinder(Random random, ILogger logger, int port, int peerBufferSize)
    {
        _random = random;
        _logger = logger;
        _port = port;
        _peerBufferSize = peerBufferSize;
    }

    public async Task<ITrackerFetcher> FindTrackerAsync(IEnumerable<IList<string>> urls)
    {
        var canceller = new CancellationTokenSource();
        var tasks = new List<Task<ITrackerFetcher>>();
        foreach (var set in urls)
        {
            foreach (string url in set.OrderBy(_ => _random.Next()))
            {
                if (url.StartsWith("udp"))
                {
                    tasks.Add(ConnectUdpAsync(url, canceller.Token));
                }
            }
        }
        ITrackerFetcher? fetcher = null;
        var readyTasks = new HashSet<Task>();
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

    private async Task<ITrackerFetcher> ConnectUdpAsync(string url, CancellationToken cancellationToken = default)
    {
        const string protocolStart = "udp://";
        var portSeparator = url.LastIndexOf(':');
        var portEnd = url.IndexOf('/', portSeparator);
        if (portEnd == -1) portEnd = url.Length;
        var port = ushort.Parse(url.AsSpan((portSeparator + 1)..portEnd));
        var addresses = await Dns.GetHostAddressesAsync(url[protocolStart.Length..portSeparator], cancellationToken);
        foreach (var address in addresses)
        {
            var client = new UdpClient();
            client.Connect(address, port);
            var udpTracker = new UdpTrackerFetcher(client, _port, _peerBufferSize);
            try
            {
                var trackerTask = udpTracker.ConnectAsync(cancellationToken);
                var ready = await Task.WhenAny(trackerTask, Task.Delay(TimeSpan.FromSeconds(1), cancellationToken));
                if (ready == trackerTask)
                {
                    return udpTracker;
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }
        throw new Exception();
    }
}
