using System.Net;
using System.Net.Sockets;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;

namespace BitTorrentClient.Protocol.Transport.Trackers;

public sealed class TrackerConnector : ITrackerConnector
{
    private readonly HttpClient _client;
    private readonly int _port;
    private readonly int _peerBufferSize;

    public TrackerConnector(HttpClient client, int port, int peerBufferSize)
    {
        _client = client;
        _port = port;
        _peerBufferSize = peerBufferSize;
    }

    public async Task<ITrackerFetcher> ConnectAsync(Uri uri, TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        return uri.Scheme switch
        {
            "udp" => await ConnectUdpAsync(uri, cancellationToken),
            "http" or "https" => await ConnectHttpAsync(uri, update, cancellationToken),
            _ => throw new NotSupportedException($"{uri.Scheme} protocol is not supported.")
        };
    }
    
    private async Task<ITrackerFetcher> ConnectUdpAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        foreach (var address in addresses)
        {
            var client = new UdpClient();
            client.Connect(address, uri.Port);
            var udpTracker = new UdpTrackerFetcher(client, _port, _peerBufferSize);
            try
            {
                var trackerTask = udpTracker.ConnectAsync(cancellationToken);
                var ready = await Task.WhenAny(trackerTask, Task.Delay(TimeSpan.FromSeconds(1), cancellationToken));
                if (ready == trackerTask)
                {
                    return udpTracker;
                }
                throw new TimeoutException();
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }
        throw new Exception();
    }

    private async Task<ITrackerFetcher> ConnectHttpAsync(Uri uri, TrackerUpdate update, CancellationToken cancellationToken = default)
    {
        var fetcher = new HttpTrackerFetcher(_client, uri, _port, _peerBufferSize);
        fetcher.InitialResponse = await fetcher.FetchAsync(update, cancellationToken);
        return fetcher;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}