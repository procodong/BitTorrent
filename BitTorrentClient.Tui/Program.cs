using System.Threading.Channels;
using BitTorrentClient.Tui;
using Microsoft.Extensions.Logging;
using BitTorrentClient.Helpers.Utility;
using BitTorrentClient.Application.Launchers;
using BitTorrentClient.Application.State;

var fileProvider = new PersistenStateManager("BitTorrentClient");
var config = await fileProvider.GetConfigAsync();
var downloads = await fileProvider.GetStateAsync();

await using var logFile = fileProvider.GetLog();
var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);
var logger = new ChannelLogger(messageChannel.Writer, logFile);

var applicationLauncher = new ApplicationLauncher(new("BT", new('0', '1', '1', '1')), config, logger);

var canceller = new CancellationTokenSource();

await using var downloadService = applicationLauncher.LaunchApplication(canceller.Token);

foreach (var download in downloads)
{
    downloadService.AddDownload(download);
}

var interfaceLauncher = new InterfaceLauncher(TimeSpan.FromSeconds(1), logger);

interfaceLauncher.LaunchInterface(downloadService, messageChannel.Reader, canceller.Token);

var closingWaiter = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    canceller.Cancel();
    e.Cancel = true;
    closingWaiter.SetResult();
};
await closingWaiter.Task;
await fileProvider.SaveStateAsync(downloadService.GetDownloads().Select(d => d.Download).ToArray());