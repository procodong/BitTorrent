using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening.Downloads;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.UserInterface.Input;

public class CommandReader
{
    private readonly RootCommand _commands;
    private readonly Command _createCommand;
    private readonly Command _removeCommand;
    private readonly ChannelWriter<Func<ICommandContext, Task>> _actionWriter;
    private readonly ILogger _logger;

    public CommandReader(RootCommand commands, Command createCommand, Command removeCommand, ChannelWriter<Func<ICommandContext, Task>> actionWriter, ILogger logger)
    {
        _commands = commands;
        _createCommand = createCommand;
        _removeCommand = removeCommand;
        _actionWriter = actionWriter;
        _logger = logger;
    }

    public static CommandReader Create(ChannelWriter<Func<ICommandContext, Task>> actionWriter, ILogger logger)
    {
        var createCommand = new Command("download")
        {
            new Argument<FileInfo>("torrent_file"),
            new Argument<DirectoryInfo>("target_directory"),
            new Option<string>("--name", "-n")
        };
        var removeCommand = new Command("remove")
        {
            new Option<string>("--name", "-n"),
            new Option<int?>("--index", "-i")
        };
        var commands = new RootCommand
        {
            createCommand,
            removeCommand
        };
        return new(commands, createCommand, removeCommand, actionWriter, logger);
    }

    public async Task ReadAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var line = await Console.In.ReadLineAsync(cancellationToken);
            if (line is null) return;
            var parsed = _commands.Parse(line);
            foreach (var error in parsed.Errors)
            {
                _logger.LogError("Parsing error: {}", error.Message);
            }
            if (parsed.Errors.Count != 0) continue;
            var command = parsed.CommandResult.Command;
            if (command == _createCommand)
            {
                var torrent = parsed.CommandResult.GetRequiredValue((Argument<FileInfo>)_createCommand.Arguments[0]);
                var targetDirectory = parsed.CommandResult.GetRequiredValue((Argument<DirectoryInfo>)_createCommand.Arguments[1]);
                var name = parsed.CommandResult.GetValue((Option<string>)_createCommand.Options[0]);

                await _actionWriter.WriteAsync(ctx => ctx.AddTorrentAsync(torrent.FullName, targetDirectory.FullName, name), cancellationToken);
            }
            else if (command == _removeCommand)
            {
                var name = parsed.CommandResult.GetValue((Option<string>)_removeCommand.Options[0]);
                var index = parsed.CommandResult.GetValue((Option<int?>)_removeCommand.Options[1]);
                if (name is not null)
                {
                    await _actionWriter.WriteAsync(ctx => ctx.RemoveTorrentAsync(name), cancellationToken);
                }
                else if (index is not null)
                {
                    await _actionWriter.WriteAsync(ctx => ctx.RemoveTorrentAsync(index.Value), cancellationToken);
                }
            }
        }
    }
}