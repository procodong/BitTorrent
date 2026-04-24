using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Api.Services;
using BitTorrentClient.Core.Presentation.PeerWire;
using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Core.Transport.PeerWire.Connecting;
using BitTorrentClient.Core.Transport.Trackers;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Launchers;
using BitTorrentClient.Engine.Models.Config;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Api.Interface;

public static class ClientLauncher
{
    public static IDownloadService LaunchClient(ClientIdentifier id, ILogger logger)
    {
        var config = new NetworkingConfig
        {
            RequestSize = 1 << 14,
            RequestQueueSize = 5,
            MaxRequestSize = 1 << 17,
            PieceSegmentSize = 1 << 17,
            PeerBufferSize = 1 << 15,
            PeerUpdateInterval = TimeSpan.FromSeconds(10),
            KeepAliveInterval = TimeSpan.FromSeconds(90),
            ReceiveTimeout = TimeSpan.FromMinutes(2),
            TransferRateResetInterval = TimeSpan.FromSeconds(5),
            PiecesBufferSize = 1 << 6
        };
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