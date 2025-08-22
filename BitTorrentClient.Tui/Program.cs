
using System.Threading.Channels;
using BitTorrentClient.Application.Launchers.Application;
using BitTorrentClient.Tui;
using BitTorrentClient.Helpers;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;

await using var logFile = LoggingFileProvider.GetLogFile("BitTorrentClient");
var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);
var logger = new ChannelLogger(messageChannel.Writer, logFile);

var applicationLauncher = new ApplicationLauncher(new("KJ", new('0', '1', '1', '1')), Config.Default, logger);

var canceller = new CancellationTokenSource();

await using var downloadService = applicationLauncher.LaunchApplication(canceller.Token);

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
