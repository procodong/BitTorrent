using BitTorrent.Application;
using BitTorrent.Application.Input;
using BitTorrent.Application.Input.Commands;
using BitTorrent.Application.Ui;
using BitTorrent.Models.Application;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Managing;
using BitTorrent.Torrents.Trackers;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

var commandChannel = Channel.CreateBounded<ICommand>(32);
var newPeerChannel = Channel.CreateBounded<PeerReceivingSubscribe>(32);
var httpClient = new HttpClient();
var config = new Config(
    TargetDownload: 100_000,
    TargetUpload: 1000,
    TargetUploadSeeding: 100_000,
    RequestSize: 1 << 14,
    RequestQueueSize: 5,
    MaxRarePieceCount: 20,
    PeerUpdateInterval: 30 * 1000,
    MaxRequestSize: 1 << 17,
    KeepAliveInterval: 90 * 1000,
    ReceiveTimeout: 2 * 60 * 1000,
    RarePiecesUpdateInterval: 60 * 1000,
    UiUpdateInterval: 1000
    );
int port = 6881;
ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
ILogger logger = factory.CreateLogger("Program");
var peerReceiver = new TrackerHandler(port, config.ReceiveTimeout, newPeerChannel.Reader);
_ = Task.Run(peerReceiver.ListenAsync);
var trackerFetcher = new TrackerFinder(new(), httpClient, port);
var downloads = new DownloadManager(newPeerChannel.Writer, config, logger, trackerFetcher);
var ui = new CliHandler();
Console.WriteLine();
var app = new ApplicationManager(commandChannel.Reader, downloads, ui, config);
var inputHandler = new InputHandler(commandChannel.Writer);
_ = Task.Run(async () =>
{
    await inputHandler.ListenAsync(Console.In, Console.Out);
});
await app.ListenAsync();
