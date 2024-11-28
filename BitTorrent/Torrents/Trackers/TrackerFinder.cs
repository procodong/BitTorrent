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
        foreach (var set in urls)
        {
            foreach (string url in set.OrderBy(_ => _random.Next()))
            {
                try
                {
                    if (url.StartsWith("udp"))
                    {
                        var tracker = await ConnectUdpAsync(url);
                        if (tracker is not null) return tracker;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        throw new NoValidTrackerException();
    }

    private async Task<ITrackerFetcher?> ConnectUdpAsync(string url)
    {
        var portSeperator = url.LastIndexOf(':');
        var portEnd = url.IndexOf('/', portSeperator);
        if (portEnd == -1) portEnd = url.Length;
        var port = ushort.Parse(url.AsSpan((portSeperator + 1)..portEnd));
        var addresses = await Dns.GetHostAddressesAsync(url[6..portSeperator]);
        var client = new UdpClient();
        client.Connect(addresses[0], port);
        var udpTracker = new UdpTrackerFetcher(client, _port);
        var timeoutTask = Task.Delay(2000);
        var ready = await Task.WhenAny(udpTracker.ConnectAsync(), timeoutTask);
        if (ready == timeoutTask) return null;
        Console.WriteLine("Found!");
        return udpTracker;
    }
}
