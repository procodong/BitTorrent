using System.Threading.Channels;
using BitTorrentClient.Api;
using BitTorrentClient.Api.Downloads;
using BitTorrentClient.Tui.Interface.Input;
using BitTorrentClient.Tui.Interface.Output;
using Microsoft.Extensions.Logging;

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
        var handler = UiHandler.Create(10);
        var updater = new UiUpdater(handler, downloadService, messageReader, new(_tickInterval));
        _ = updater.ListenAsync(cancellationToken);

        var commandReader = CommandReader.Create(downloadService, _logger);
        _ = commandReader.ReadAsync(cancellationToken);
    }

}