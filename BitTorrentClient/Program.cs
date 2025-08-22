using BitTorrentClient.Models.Application;
using BitTorrentClient.Application.Launchers.Application;

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
var canceller = new CancellationTokenSource();
var launcher = new ApplicationLauncher("BitTorrentClient", new("KJ", new('0', '1', '1', '1')), config);

Console.CancelKeyPress += (_, e) =>
{
    canceller.Cancel();
    e.Cancel = true;
};
try
{
    await launcher.LaunchApplication(canceller.Token);
}
catch (OperationCanceledException) { }
