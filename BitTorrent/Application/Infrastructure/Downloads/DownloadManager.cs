using BitTorrentClient.Models.Application;
using BitTorrentClient.Helpers;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.UserInterface.Input;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public class DownloadManager : IAsyncDisposable
{
    private readonly DownloadCollection _downloads;
    private readonly ChannelWriter<IEnumerable<DownloadUpdate>> _updateWriter;

    public DownloadManager(DownloadCollection downloads, ChannelWriter<IEnumerable<DownloadUpdate>> updateWriter)
    {
        _downloads = downloads;
        _updateWriter = updateWriter;
    }

    public async Task ListenAsync(ChannelReader<Func<ICommandContext, Task>> commands, CancellationToken cancellationToken = default)
    {
        var commandTask = commands.ReadAsync(cancellationToken).AsTask();
        var intervalTask = Task.Delay(_downloads.Config.UiUpdateInterval, cancellationToken);
        while (true)
        {
            Task ready = await Task.WhenAny(commandTask, intervalTask);
            if (ready == commandTask)
            {
                var command = await commandTask;
                try
                {
                    await command(_downloads);
                }
                catch (Exception exc)
                {
                    _downloads.Logger.LogError("handling user command", exc);
                }
                commandTask = commands.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == intervalTask)
            {
                if (_downloads.HasUpdates)
                {
                    await _updateWriter.WriteAsync(_downloads.GetUpdates(), cancellationToken);
                }
                intervalTask = Task.Delay(_downloads.Config.UiUpdateInterval, cancellationToken);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return _downloads.DisposeAsync();
    }
}
