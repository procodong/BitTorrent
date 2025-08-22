using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Cli.Interface.Input;
using BitTorrentClient.Cli.Interface.Output;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitTorrentClient.Cli;

internal class InterfaceLauncher
{
    private readonly ILogger _logger;

    public InterfaceLauncher(ILogger logger)
    {
        _logger = logger;
    }

    public void LaunchInterface(IDownloadRepository torrentRepository, ChannelReader<(LogLevel, string)> messageReader, ChannelReader<IEnumerable<DownloadUpdate>> updateReader, CancellationToken cancellationToken = default)
    {
        var ui = new Table();
        var handler = UiHandler.Create(ui, 10);
        var updater = new UiUpdater(handler, messageReader, updateReader);
        _ = updater.ListenAsync(cancellationToken);

        var commandReader = CommandReader.Create(torrentRepository, _logger);
        _ = Task.Run(() => commandReader.ReadAsync(cancellationToken), cancellationToken);
    }

}