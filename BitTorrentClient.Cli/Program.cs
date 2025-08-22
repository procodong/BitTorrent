
using System.Threading.Channels;
using BitTorrentClient.Api;
using BitTorrentClient.Cli;
using BitTorrentClient.Data;
using BitTorrentClient.Helpers.Utility;
using BitTorrentClient.PersistentState;
using Microsoft.Extensions.Logging;
using Spectre.Console;

var fileProvider = new PersistentStateManager("BitTorrentClient");
await using var logFile = fileProvider.GetLog();
var config = await fileProvider.GetConfigAsync();
var messageChannel = Channel.CreateBounded<(LogLevel, string)>(8);
var logger = new ChannelLogger(messageChannel.Writer, logFile);
var logReader = new LogReader();
_ = logReader.ReadLogs(messageChannel.Reader);

var canceller = new CancellationTokenSource();
var downloadService = ClientLauncher.LaunchClient(new("BT", new('0', '1', '1', '1')), config, logger, canceller.Token);

if (args.Length != 2)
{
    Console.WriteLine("Please provide the torrent file path and save path");
    return;
}
IDownloadController download = await downloadService.AddDownloadAsync(new(args[0]), new(args[1]));

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