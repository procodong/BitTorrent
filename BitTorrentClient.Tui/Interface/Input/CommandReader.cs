using System.CommandLine;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Tui.Interface.Input;

public class CommandReader
{
    private readonly RootCommand _commands;
    private readonly Command _createCommand;
    private readonly Command _removeCommand;
    private readonly IDownloadService _downloadService;
    private readonly ILogger _logger;
    private readonly List<DownloadData> _downloads;

    private CommandReader(RootCommand commands, Command createCommand, Command removeCommand, IDownloadService actionWriter, ILogger logger)
    {
        _commands = commands;
        _createCommand = createCommand;
        _removeCommand = removeCommand;
        _downloadService = actionWriter;
        _logger = logger;
        _downloads = [];
    }

    public static CommandReader Create(IDownloadService actionWriter, ILogger logger)
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

                var id = await _downloadService.AddDownloadAsync(torrent, targetDirectory, name);
                _downloads.Add(new(id, name ?? targetDirectory.FullName));
            }
            else if (command == _removeCommand)
            {
                var name = parsed.CommandResult.GetValue((Option<string>)_removeCommand.Options[0]);
                var index = parsed.CommandResult.GetValue((Option<int?>)_removeCommand.Options[1]);
                int downloadIndex = index ?? _downloads.FindIndex(d => d.Name == name);
                if (downloadIndex == -1 || downloadIndex >= _downloads.Count)
                {
                    _logger.LogError("Could not find download {}", (object?)name ?? index);
                }
                await _downloadService.RemoveDownloadAsync(_downloads[downloadIndex].Id);
                _downloads.RemoveAt(downloadIndex);
            }
        }
    }
}

readonly record struct DownloadData(ReadOnlyMemory<byte> Id, string Name);