using System.CommandLine;
using System.CommandLine.Parsing;
using BitTorrentClient.Api.Interface;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Tui.Interface.Input;

public sealed class CommandReader
{
    private readonly RootCommand _commands;
    private readonly Command _createCommand;
    private readonly Command _removeCommand;
    private readonly Command _pauseCommand;
    private readonly Command _continueCommand;
    private readonly IDownloadService _downloadService;
    private readonly ILogger _logger;
    private readonly List<IDownloadHandle> _downloads;

    private CommandReader(RootCommand commands, Command createCommand, Command removeCommand, Command pauseCommand, Command continueCommand, IDownloadService actionWriter, List<IDownloadHandle> downloads, ILogger logger)
    {
        _commands = commands;
        _createCommand = createCommand;
        _removeCommand = removeCommand;
        _pauseCommand = pauseCommand;
        _continueCommand = continueCommand;
        _downloadService = actionWriter;
        _logger = logger;
        _downloads = downloads;
    }

    public static CommandReader Create(IDownloadService actionWriter, List<IDownloadHandle> downloads, ILogger logger)
    {
        var createCommand = new Command("download")
        {
            new Argument<FileInfo>("torrent_file"),
            new Argument<DirectoryInfo>("target_directory"),
            new Option<string>("--name", "-n"),
            new Option<int>("--limit-download", "-ld"),
            new Option<int>("--limit-upload", "-lu"),
            new Option<int>("--limit-peers", "-lp"),
            new Option<string>("--order", "-o")
        };
        var removeCommand = new Command("remove")
        {
            new Option<string>("--name", "-n"),
            new Option<int?>("--index", "-i")
        };
        var pauseCommand = new Command("pause")
        {
            new Option<string>("--name", "-n"),
            new Option<int?>("--index", "-i")
        };
        var continueCommand = new Command("continue")
        {
            new Option<string>("--name", "-n"),
            new Option<int?>("--index", "-i")
        };
        var commands = new RootCommand
        {
            createCommand,
            removeCommand,
            pauseCommand,
            continueCommand
        };
        return new(commands, createCommand, removeCommand, pauseCommand, continueCommand, actionWriter, downloads, logger);
    }

    public async Task ReadAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) return;
            var parsed = _commands.Parse(line);
            foreach (var error in parsed.Errors)
            {
                _logger.LogError("Parsing error: {}", error.Message);
            }
            if (parsed.Errors.Count != 0) continue;
            var command = parsed.CommandResult.Command;
            await ExecuteCommandAsync(parsed, command, cancellationToken);
        }
    }

    private async Task ExecuteCommandAsync(ParseResult parsed, Command command, CancellationToken cancellationToken = default)
    {
        if (command == _createCommand)
        {
            var torrent = parsed.CommandResult.GetRequiredValue((Argument<FileInfo>)_createCommand.Arguments[0]);
            var targetDirectory = parsed.CommandResult.GetRequiredValue((Argument<DirectoryInfo>)_createCommand.Arguments[1]);
            var name = parsed.CommandResult.GetValue((Option<string>)_createCommand.Options[0]);
            var downloadLimit = parsed.CommandResult.GetValue((Option<int>)_createCommand.Options[1]);
            var uploadLimit = parsed.CommandResult.GetValue((Option<int>)_createCommand.Options[2]);
            var peerLimit = parsed.CommandResult.GetValue((Option<int>)_createCommand.Options[3]);
            var order = parsed.CommandResult.GetValue((Option<string>)_createCommand.Options[4]);
            var settings = new DownloadSettings
            {
                Name = name
            };
            if (downloadLimit != 0) settings.DownloadLimit = downloadLimit;
            if (uploadLimit != 0) settings.UploadLimit = uploadLimit;
            if (peerLimit != 0) settings.MaxParallelPeers = peerLimit;
            switch (order)
            {
                case "sequential":
                    settings.Strategy = PieceSelectionStrategy.Sequential;
                    break;
                case "rarest":
                    settings.Strategy = PieceSelectionStrategy.RarestFirst;
                    break;
            }
            try
            {
                var download = await _downloadService.AddDownloadAsync(torrent, targetDirectory, settings, cancellationToken);
                _downloads.Add(download);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Creating download {}", ex);
            }
        }
        else if (command == _removeCommand)
        {
            if (TryFindDownload(parsed.CommandResult, command.Options, out var download))
            {
                try
                {
                    _downloadService.RemoveDownload(download.Download.Identifier);
                    _downloads.Remove(download);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Removing download {}", ex);
                }
            }
        }
        else if (command == _pauseCommand)
        {
            if (TryFindDownload(parsed.CommandResult, command.Options, out var download))
            {
                try
                {
                    await download.PauseAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Pausing command {}", ex);
                }
            }
        }
        else if (command == _continueCommand)
        {
            if (TryFindDownload(parsed.CommandResult, command.Options, out var download))
            {
                try
                {
                    await download.ResumeAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Resuming command {}", ex);
                }
            }
        }
    }

    private bool TryFindDownload(CommandResult result, IList<Option> options, out IDownloadHandle download)
    {

        var name = result.GetValue((Option<string>)options[0]);
        var index = result.GetValue((Option<int?>)options[1]);
        var downloadIndex = index ?? _downloads.FindIndex(d => d.Download.Name == name);
        if (downloadIndex == -1 || downloadIndex >= _downloads.Count)
        {
            _logger.LogError("Could not find download {}", (object?)name ?? index);
            download = null!;
            return false;
        }
        download = _downloads[downloadIndex];
        return true;
    }
}