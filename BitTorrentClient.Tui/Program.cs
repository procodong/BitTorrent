using System.Threading.Channels;
using BitTorrentClient.Api.Downloads;
using BitTorrentClient.Api.PersistentState;
using BitTorrentClient.Tui;
using Microsoft.Extensions.Logging;
using BitTorrentClient.Helpers.Utility;

var fileProvider = new PersistentStateManager("BitTorrentClient");
var config = await fileProvider.GetConfigAsync();
var downloads = await fileProvider.GetStateAsync();

await using var logFile = fileProvider.GetLog();
var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);
var logger = new ChannelLogger(messageChannel.Writer, logFile);

await using var downloadService = ClientLauncher.LaunchClient(new(('B', 'T'), new('0', '1', '1', '1')), config, logger);

var downloadHandles = new List<IDownloadHandle>();
foreach (var download in downloads)
{
    try
    {
        var handle = downloadService.AddDownload(download);
        downloadHandles.Add(handle);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to add download {}", ex);
    }
}

var interfaceLauncher = new InterfaceLauncher(TimeSpan.FromSeconds(1), logger);

var interfaceCanceller = new CancellationTokenSource();

interfaceLauncher.LaunchInterface(downloadService, downloadHandles, messageChannel.Reader, interfaceCanceller.Token);

var closingWaiter = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    interfaceCanceller.Cancel();
    e.Cancel = true;
    closingWaiter.SetResult();
};
await closingWaiter.Task;
await fileProvider.SaveStateAsync(downloadService.GetDownloads());
