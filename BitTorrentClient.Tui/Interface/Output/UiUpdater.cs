using System.Threading.Channels;
using BitTorrentClient.Api.Downloads;
using BitTorrentClient.Helpers.DataStructures;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Tui.Interface.Output;
public sealed class UiUpdater
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
        var taskListener = new TaskListener<EventType>(cancellationToken);
        taskListener.AddTask(EventType.Message, () => _messageReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.Update, () => _tickTimer.WaitForNextTickAsync(cancellationToken).AsTask());
        while (true)
        {
            var (eventType, readyTask) = await taskListener.WaitAsync();

            switch (eventType)
            {
                case EventType.Message:
                    var (level, message) = await (Task<(LogLevel, string)>)readyTask;
                    _uiHandler.AddMessage(level, message);
                    break;
                case EventType.Update:
                    var update = await (Task<bool>)readyTask;
                    if (!update) break;
                    _uiHandler.Update(_downloadService.GetDownloadUpdates());
                    break;

            }
        }
    }

    enum EventType
    {
        Message,
        Update
    }
}
