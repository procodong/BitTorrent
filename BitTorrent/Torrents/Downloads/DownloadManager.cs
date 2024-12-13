using BitTorrent.Application.Input;
using BitTorrent.Models.Application;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Downloads;
public class DownloadManager : IAsyncDisposable, IDisposable
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
                    _downloads.Logger.LogError("Error handling user command: {}", exc);
                }
                commandTask = commands.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == intervalTask)
            {
                if (_downloads.HasUpdates)
                {
                    await _updateWriter.WriteAsync(_downloads.GetUpdates());
                }
                intervalTask = Task.Delay(_downloads.Config.UiUpdateInterval, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _downloads.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _downloads.DisposeAsync();
    }
}
