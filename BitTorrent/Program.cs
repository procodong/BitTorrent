using BitTorrent.Application.Input;
using BitTorrent.Application.Ui;
using BitTorrent.Models.Application;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Trackers;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

var config = new Config(
    TargetDownload: 100_000,
    TargetUpload: 1000,
    TargetUploadSeeding: 100_000,
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
ILogger logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Program");

var commandChannel = Channel.CreateBounded<Func<ICommandContext, Task>>(32);
var newDownloadChannel = Channel.CreateBounded<PeerReceivingSubscribe>(32);
var updateChannel = Channel.CreateBounded<IEnumerable<DownloadUpdate>>(32);

var peerReceiver = new TrackerHandler(port, config.ReceiveTimeout, logger);

_ = peerReceiver.ListenAsync(newDownloadChannel.Reader);

var inputHandler = new InputHandler(commandChannel.Writer);

new Thread(async () =>
{
    await inputHandler.ListenAsync(Console.In, Console.Out);
}).Start();

var ui = new CliHandler();
Console.WriteLine();
var uiUpdater = new UiUpdater(ui);

_ = uiUpdater.ListenAsync(updateChannel.Reader);

var canceller = new CancellationTokenSource();
var trackerFetcher = new TrackerFinder(new(), logger, port);
var downloads = new DownloadCollection(newDownloadChannel.Writer, config, logger, trackerFetcher);
await using var downloadManager = new DownloadManager(downloads, updateChannel.Writer);

Console.CancelKeyPress += (_, e) =>
{
    canceller.Cancel();
    e.Cancel = true;
};
try
{
    await downloadManager.ListenAsync(commandChannel.Reader, canceller.Token);
}
catch (OperationCanceledException) { }
