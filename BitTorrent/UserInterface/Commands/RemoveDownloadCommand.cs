using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening.Downloads;
using Spectre.Console.Cli;

namespace BitTorrentClient.UserInterface.Commands;

public class RemoveDownloadCommand : AsyncCommand<RemoveDownloadCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        public string? DownloadName { get; init; }

        [CommandArgument(0, "[index]")]
        public int? DownloadIndex { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var channel = (ChannelWriter<Func<ICommandContext, Task>>)context.Data!;
        if (settings.DownloadName is not null)
        {
            await channel.WriteAsync(v => v.RemoveTorrentAsync(settings.DownloadName));
        }
        else if (settings.DownloadIndex is not null)
        {
            await channel.WriteAsync(v => v.RemoveTorrentAsync(settings.DownloadIndex.Value));
        }
        return 0;
    }

}