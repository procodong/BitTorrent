using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using BitTorrentClient.Protocol.Transport.Trackers.Exceptions;

namespace BitTorrentClient.Protocol.Transport.Trackers;
public class TrackerFinder : ITrackerFinder
{
    private readonly int _port;
    private readonly Random _random;
    private readonly ILogger _logger;

    public TrackerFinder(Random random, ILogger logger, int port)
    {
        _random = random;
        _logger = logger;
        _port = port;
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
        while (tasks.Count != 0)
        {
            var tracker = await Task.WhenAny(tasks.Where(t => !readyTasks.Contains(t)));
            try
            {
                var workingTracker = await tracker;
                if (fetcher is null)
                {
                    fetcher = workingTracker;
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
                    _logger.LogError("Tracker exception connecting to tracker: {}", ex.Message);
                }
                readyTasks.Add(tracker);
            }
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
            var udpTracker = new UdpTrackerFetcher(client, _port);
            try
            {
                await udpTracker.ConnectAsync(cancellationToken);
                return udpTracker;
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
