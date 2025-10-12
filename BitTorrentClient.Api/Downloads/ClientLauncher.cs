using System.Net;
using System.Net.Sockets;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Api.PersistentState;
using BitTorrentClient.Api.Services;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Launchers;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using BitTorrentClient.Protocol.Transport.Trackers;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Api.Downloads;

public static class ClientLauncher
{
    public static IDownloadService LaunchClient(ClientIdentifier id, ConfigBuilder configBuilder, ILogger logger)
    {
        var config = configBuilder.Build(Config.Default);
        var clientId = PeerIdGenerator.GeneratePeerId(new string([id.ClientId.Item1, id.ClientId.Item2]), id.ClientVersion.ToString());

        var (port, socket) = FindPort();
        var httpClient = new HttpClient();
        var trackerConnector = new TrackerConnector(httpClient, port, config.PeerBufferSize);
        var trackerFinder = new TrackerFinder(trackerConnector, logger);
        var downloadLauncher = new DownloadLauncher(logger);
        var downloads = new DownloadCollection(clientId, config, trackerFinder, downloadLauncher);
        var peerReceiver = new TcpPeerReceiver(socket, config.PeerBufferSize);

        var canceller = new CancellationTokenSource();
        var clientTask = ListenAsync(downloads, peerReceiver, canceller.Token);
        return new DownloadService(downloads, canceller, clientTask);
    }
    
    private static async Task ListenAsync(DownloadCollection downloads, TcpPeerReceiver peerReceiver, CancellationToken cancellationToken = default)
    {
        await using (downloads)
        {
            using (peerReceiver)
            {
                while (true)
                {
                    var peer = await peerReceiver.ReceivePeerAsync(cancellationToken);
                    _ = downloads.AddPeerAsync(peer, cancellationToken);
                }
            }
        }
    }

    private static (int, TcpListener) FindPort()
    {
        SocketException? socketException = null;
        for (var i = 0; i < 5; i++)
        {
            var port = Random.Shared.Next(49152, 65535);
            var listener = new TcpListener(IPAddress.Any, port);
            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                socketException = ex;
                listener.Dispose();
                continue;
            }
            return (port, listener);
        }
        throw socketException!;
    }
}