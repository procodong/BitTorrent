
using System.Threading.Channels;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Launchers;
using BitTorrentClient.Application.State;
using BitTorrentClient.Cli;
using BitTorrentClient.Helpers.Utility;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;
using Spectre.Console;

var fileProvider = new PersistenStateManager("BitTorrentClient");
await using var logFile = fileProvider.GetLog();
var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);
var logger = new ChannelLogger(messageChannel.Writer, logFile);
var logReader = new LogReader();
_ = logReader.ReadLogs(messageChannel.Reader);

var applicationLauncher = new ApplicationLauncher(new("BT", new('0', '1', '1', '1')), Config.Default, logger);
var canceller = new CancellationTokenSource();
var downloadService = applicationLauncher.LaunchApplication(canceller.Token);

if (args.Length != 2)
{
    Console.WriteLine("Please provide the torrent file path and save path");
    return;
}
var download = await downloadService.AddDownloadAsync(new(args[0]), new(args[1]));

while (true)
{
    await AnsiConsole.Progress()
        .AutoRefresh(true)
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask(download.State.DownloadName);
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                var update = download.State;
                if (update.ExecutionState == DownloadExecutionState.PausedAutomatically)
                {
                    break;
                }
                task.Value = update.Progress;
            }
        });
    Console.WriteLine(logReader.LatestMessage);
    Console.WriteLine("Press enter to resume download");
    Console.ReadLine();
    await download.ResumeAsync();
}