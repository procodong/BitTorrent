using System.Threading.Channels;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitTorrentClient.UserInterface.Output;
public class UiUpdater
{
    private readonly UiHandler _uiHandler;
    private readonly ChannelReader<(LogLevel, string)> _messsageReader;
    private readonly ChannelReader<IEnumerable<DownloadUpdate>> _updateReader;

    public UiUpdater(UiHandler uiHandler, ChannelReader<(LogLevel, string)> messageReader, ChannelReader<IEnumerable<DownloadUpdate>> updateReader)
    {
        _uiHandler = uiHandler;
        _messsageReader = messageReader;
        _updateReader = updateReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        var messageTask = _messsageReader.ReadAsync(cancellationToken).AsTask();
        var updateTask = _updateReader.ReadAsync(cancellationToken).AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(messageTask, updateTask);
            if (ready == messageTask)
            {
                var (level, message) = await messageTask;
                _uiHandler.AddMessage(level, message);
                messageTask = _messsageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == updateTask)
            {
                var update = await updateTask;
                _uiHandler.Update(update);
                updateTask = _updateReader.ReadAsync(cancellationToken).AsTask();
            }
        }
    }
}
