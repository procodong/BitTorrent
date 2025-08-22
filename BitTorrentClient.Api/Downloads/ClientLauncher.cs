using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Api.PersistentState;
using BitTorrentClient.Api.Services;
using BitTorrentClient.Engine.Events.Handling;
using BitTorrentClient.Engine.Events.Listening;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Launchers;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using BitTorrentClient.Protocol.Transport.Trackers;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Api.Downloads;

public static class ClientLauncher
{
    public static IDownloadService LaunchClient(ClientIdentifier id, ConfigBuilder configBuilder, ILogger logger, CancellationToken cancellationToken)
    {
        var config = configBuilder.Build(Config.Default);
        int peerBufferSize = config.RequestSize + Unsafe.SizeOf<BlockShareHeader>() + sizeof(int) + sizeof(byte);

        var (port, socket) = FindPort();

        var clientId = PeerIdGenerator.GeneratePeerId(new string([id.ClientId.Item1, id.ClientId.Item2]), id.ClientVersion.ToString());
        var trackerFetcher = new TrackerFinder(logger, port, peerBufferSize);
        var downloadLauncher = new DownloadLauncher(logger);
        var downloads = new DownloadCollection(clientId, config, trackerFetcher, downloadLauncher);
        var downloadEventHandler = new DownloadEventHandler(downloads);
        var peerReceiver = new TcpPeerReceiver(socket, peerBufferSize);
        var downloadEventListener = new DownloadEventListener(downloadEventHandler, peerReceiver);
        _ = downloadEventListener.ListenAsync(cancellationToken);
        return new DownloadService(downloads, logger);
    }

    private static (int, TcpListener) FindPort()
    {
        SocketException? socketException = null;
        for (int i = 0; i < 5; i++)
        {
            int port = Random.Shared.Next(49152, 65535);
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