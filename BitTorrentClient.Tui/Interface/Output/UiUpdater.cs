using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Downloads.Interface;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Tui.Interface.Output;
public class UiUpdater
{
    private readonly UiHandler _uiHandler;
    private readonly ChannelReader<(LogLevel, string)> _messageReader;
    private readonly PeriodicTimer _tickTimer;
    private readonly IDownloadService _downloadService;

    public UiUpdater(UiHandler uiHandler, IDownloadService downloadService, ChannelReader<(LogLevel, string)> messageReader, PeriodicTimer tickTimer)
    {
        _uiHandler = uiHandler;
        _messageReader = messageReader;
        _tickTimer = tickTimer;
        _downloadService = downloadService;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        var messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        var updateTask = _tickTimer.WaitForNextTickAsync(cancellationToken).AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(messageTask, updateTask);
            if (ready == messageTask)
            {
                var (level, message) = await messageTask;
                _uiHandler.AddMessage(level, message);
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == updateTask)
            {
                var update = await updateTask;
                if (!update) break;
                _uiHandler.Update(_downloadService.GetDownloads().Select(d => d.State));
                updateTask = _tickTimer.WaitForNextTickAsync(cancellationToken).AsTask();
            }
        }
    }
}
