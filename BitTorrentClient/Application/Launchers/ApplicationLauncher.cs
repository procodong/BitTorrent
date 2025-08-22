using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using BitTorrentClient.Application.Events.Handling;
using BitTorrentClient.Application.Events.Listening;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Downloads.Interface;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
using BitTorrentClient.Protocol.Transport.Trackers;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Launchers;

public class ApplicationLauncher
{
    private readonly PeerIdentifier _applicationId;
    private readonly Config _config;
    private readonly ILogger _logger;

    public ApplicationLauncher(PeerIdentifier id, Config config, ILogger logger)
    {
        _applicationId = id;
        _config = config;
        _logger = logger;
    }

    public IDownloadService LaunchApplication(CancellationToken cancellationToken)
    {
        int peerBufferSize = _config.RequestSize + Unsafe.SizeOf<BlockShareHeader>() + sizeof(int) + sizeof(byte);

        var (port, socket) = FindPort();

        var peerIdGenerator = new PeerIdGenerator(_applicationId);
        var trackerFetcher = new TrackerFinder(_logger, port, peerBufferSize);
        var downloadLauncher = new DownloadLauncher(_logger);
        var downloads = new DownloadCollection(peerIdGenerator, _config, trackerFetcher, downloadLauncher);
        var downloadEventHandler = new DownloadEventHandler(downloads);
        var peerReceiver = new TcpPeerReceiver(socket, peerBufferSize);
        var downloadEventListener = new DownloadEventListener(downloadEventHandler, peerReceiver);
        _ = downloadEventListener.ListenAsync(cancellationToken);
        return new DownloadService(downloads, _logger);
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
                continue;
            }
            return (port, listener);
        }
        throw socketException!;
    }
}