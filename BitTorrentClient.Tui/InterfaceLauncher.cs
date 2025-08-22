using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Tui.Interface.Input;
using BitTorrentClient.Tui.Interface.Output;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitTorrentClient.Tui;

internal class InterfaceLauncher
{
    private readonly ILogger _logger;
    private readonly TimeSpan _tickInterval;

    public InterfaceLauncher(TimeSpan tickInterval, ILogger logger)
    {
        _tickInterval = tickInterval;
        _logger = logger;
    }

    public void LaunchInterface(IDownloadService downloadService, ChannelReader<(LogLevel, string)> messageReader, CancellationToken cancellationToken = default)
    {
        var ui = new Table();
        var handler = UiHandler.Create(ui, 10);
        var updater = new UiUpdater(handler, downloadService, messageReader, new(_tickInterval));
        _ = updater.ListenAsync(cancellationToken);

        var commandReader = CommandReader.Create(downloadService, _logger);
        _ = commandReader.ReadAsync(cancellationToken);
    }

}