using BitTorrent.Application.Input;
using BitTorrent.Models.Application;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class DownloadManager
{
    private readonly DownloadCollection _downloads;
    private readonly ChannelWriter<IEnumerable<DownloadUpdate>> _updateWriter;

    public DownloadManager(DownloadCollection downloads, ChannelWriter<IEnumerable<DownloadUpdate>> updateWriter)
    {
        _downloads = downloads;
        _updateWriter = updateWriter;
    }

    public async Task ListenAsync(ChannelReader<Func<ICommandContext, Task>> commands)
    {
        var commandTask = commands.ReadAsync().AsTask();
        var intervalTask = Task.Delay(_downloads.Config.UiUpdateInterval);
        while (true)
        {
            Task ready = Task.WhenAny(commandTask, intervalTask);
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
                commandTask = commands.ReadAsync().AsTask();
            }
            else if (ready == intervalTask)
            {
                if (_downloads.HasUpdates)
                {
                    await _updateWriter.WriteAsync(_downloads.GetUpdates());
                }
                intervalTask = Task.Delay(_downloads.Config.UiUpdateInterval);
            }
        }
    }
}
