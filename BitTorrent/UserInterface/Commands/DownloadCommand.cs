using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening.Downloads;
using Spectre.Console.Cli;

namespace BitTorrentClient.UserInterface.Commands;

public class DownloadCommand : AsyncCommand<DownloadCommand.Settings>
{

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<torrent_path>")]
        public required string TorrentPath { get; init; }

        [CommandArgument(0, "<save_path>")]
        public required string SavePath { get; init; }

        [CommandOption("-n|--name <name>")]
        public string? Name { get; set; }
    }
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var channel = (ChannelWriter<Func<ICommandContext, Task>>)context.Data!;
        await channel.WriteAsync(v => v.AddTorrentAsync(settings.TorrentPath, settings.SavePath, settings.Name));
        return 0;
    }
}
