using BitTorrent.Torrents.Trackers.Errors;
using BitTorrent.Torrents.Trackers.UdpTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Trackers;
public class TrackerFinder(Random random, HttpClient httpClient, int port) : ITrackerFinder
{
    private readonly int _port = port;
    private readonly Random _random = random;
    private readonly HttpClient _httpClient = httpClient;

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
        while (tasks.Count != 0)
        {
            var tracker = await Task.WhenAny(tasks);
            try
            {
                var workingTracker = await tracker;
                canceller.Cancel();
                return workingTracker;
            }
            catch
            {
                tasks.Remove(tracker);
            }
        }
        throw new NoValidTrackerException();
    }

    private async Task<ITrackerFetcher> ConnectUdpAsync(string url, CancellationToken cancellationToken = default)
    {
        const string PROTOCOL_START = "udp://";
        var portSeperator = url.LastIndexOf(':');
        var portEnd = url.IndexOf('/', portSeperator);
        if (portEnd == -1) portEnd = url.Length;
        var port = ushort.Parse(url.AsSpan((portSeperator + 1)..portEnd));
        var addresses = await Dns.GetHostAddressesAsync(url[PROTOCOL_START.Length..portSeperator], cancellationToken);
        var client = new UdpClient();
        client.Connect(addresses[0], port);
        var udpTracker = new UdpTrackerFetcher(client, _port);
        await udpTracker.ConnectAsync(cancellationToken);
        return udpTracker;
    }
}
