using System.Threading.Channels;
using BitTorrentClient.Api.Downloads;
using BitTorrentClient.Api.PersistentState;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Tui;
using Microsoft.Extensions.Logging;
using BitTorrentClient.Helpers.Utility;

var fileProvider = new PersistentStateManager("BitTorrentClient");
var config = await fileProvider.GetConfigAsync();
var downloads = await fileProvider.GetStateAsync();

await using var logFile = fileProvider.GetLog();
var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);
var logger = new ChannelLogger(messageChannel.Writer, logFile);

var canceller = new CancellationTokenSource();

await using var downloadService = ClientLauncher.LaunchClient(new(('B', 'T'), new('0', '1', '1', '1')), config, logger, canceller.Token);


foreach (var download in downloads)
{
    _ = downloadService.AddDownloadAsync(download).Catch(ex => logger.LogError(ex, "Failed to add download {}", ex));;
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
await fileProvider.SaveStateAsync(downloadService.GetDownloads().Select(d => d.Download));
