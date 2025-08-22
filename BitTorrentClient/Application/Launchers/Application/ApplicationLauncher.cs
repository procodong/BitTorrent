using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Application.Events.Handling.Downloads;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Application.Launchers.Downloads.Default;
using BitTorrentClient.Helpers;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
using BitTorrentClient.Protocol.Transport.Trackers;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitTorrentClient.Application.Launchers.Application;

public class ApplicationLauncher
{
    private readonly string _applicationName;
    private readonly PeerIdentifier _applicationId;
    private readonly Config _config;

    public ApplicationLauncher(string name, PeerIdentifier id, Config config)
    {
        _applicationName = name;
        _applicationId = id;
        _config = config;
    }

    public async Task LaunchApplication(CancellationToken cancellationToken)
    {
        int peerBufferSize = _config.RequestSize + 13;

        var commandChannel = Channel.CreateBounded<Func<IDownloadRepository, Task>>(8);
        var updateChannel = Channel.CreateBounded<IEnumerable<DownloadUpdate>>(8);
        var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);

        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _applicationName, "logs");
        var dir = Directory.CreateDirectory(logPath);
        var logFile = File.Open(Path.Combine(logPath, "error.log"), new FileStreamOptions()
        {
            Mode = FileMode.OpenOrCreate,
            Access = FileAccess.ReadWrite,
            Options = FileOptions.Asynchronous
        });
        logFile.Seek(0, SeekOrigin.End);
        await using var logger = new ChannelLogger(messageChannel, new(logFile));
        logger.LogInformation("Logs are at {}", logPath);

        var (port, socket) = FindPort();

        var peerIdGenerator = new PeerIdGenerator(_applicationId);
        var trackerFetcher = new TrackerFinder(new(), logger, port, peerBufferSize);
        var downloadLauncher = new DownloadLauncher(logger);
        var downloads = new DownloadCollection(peerIdGenerator, _config, trackerFetcher, downloadLauncher);
        var downloadEventHandler = new DownloadEventHandler(downloads, updateChannel.Writer, logger);
        var peerReceiver = new TcpPeerReceiver(socket, peerBufferSize);
        var downloadEventListener = new DownloadEventListener(downloadEventHandler, peerReceiver, commandChannel.Reader, _config.UiUpdateInterval, logger);
        await downloadEventListener.ListenAsync(cancellationToken);
    }

    private (int, TcpListener) FindPort()
    {
        while (true)
        {
            int port = Random.Shared.Next(49152, 65535);
            var listener = new TcpListener(IPAddress.Any, port);
            try
            {
                listener.Start();
            }
            catch (SocketException)
            {
                continue;
            }
            return (port, listener);
        }
    }
}