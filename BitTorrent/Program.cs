using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Trackers;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using BitTorrentClient.UserInterface.Input;
using BitTorrentClient.UserInterface.Output;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Transport.Trackers;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Events.Handling.Downloads;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
using System.Net.Sockets;
using System.Net;
using BitTorrentClient.Application.Launchers.Downloads.Default;

var config = new Config(
    TargetDownload: int.MaxValue,
    TargetUpload: 100_000,
    TargetUploadSeeding: 10_000_000,
    RequestSize: 1 << 14,
    RequestQueueSize: 5,
    MaxRarePieceCount: 20,
    PeerUpdateInterval: 10 * 1000,
    MaxRequestSize: 1 << 17,
    KeepAliveInterval: 90 * 1000,
    ReceiveTimeout: 2 * 60 * 1000,
    UiUpdateInterval: 1000,
    PieceSegmentSize: 1 << 17,
    MaxParallelPeers: 30,
    TransferRateResetInterval: 10
    );
int port = 6881;
int peerBufferSize = config.RequestSize + 13;
ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Program");

var commandChannel = Channel.CreateBounded<Func<ICommandContext, Task>>(32);
var updateChannel = Channel.CreateBounded<IEnumerable<DownloadUpdate>>(32);

var inputHandler = new InputHandler(commandChannel.Writer);

_ = Task.Run(async () =>
{
    await inputHandler.ListenAsync(Console.In, Console.Out);
});

var ui = new CliHandler();
Console.WriteLine();
var uiUpdater = new UiUpdater(ui);

_ = Task.Run(() => uiUpdater.ListenAsync(updateChannel.Reader).ConfigureAwait(false));

var canceller = new CancellationTokenSource();
var peerIdGenerator = new PeerIdGenerator("BT", 1001);
var trackerFetcher = new TrackerFinder(new(), logger, port, peerBufferSize);
var downloadLauncher = new DownloadLauncher(logger);
var downloads = new DownloadCollection(peerIdGenerator, config, trackerFetcher, downloadLauncher);
var downloadEventHandler = new DownloadEventHandler(downloads, updateChannel.Writer);
var peerSocket = new TcpListener(IPAddress.Any, port);
peerSocket.Start();
var peerReceiver = new TcpPeerReceiver(peerSocket, peerBufferSize);
var downloadEventListener = new DownloadEventListener(downloadEventHandler, peerReceiver, commandChannel.Reader, config.UiUpdateInterval, logger);

Console.CancelKeyPress += (_, e) =>
{
    canceller.Cancel();
    e.Cancel = true;
};
try
{
    await downloadEventListener.ListenAsync(canceller.Token);
}
catch (OperationCanceledException) { }
