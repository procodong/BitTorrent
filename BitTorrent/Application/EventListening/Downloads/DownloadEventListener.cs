using System.Threading.Channels;
using BitTorrentClient.UserInterface.Input;
using Microsoft.Testing.Platform.Logging;

namespace BitTorrentClient.Application.EventListening.Downloads;

public class DownloadEventListener
{
    private readonly IDownloadEventHandler _hander;
    private readonly ChannelReader<Func<ICommandContext, Task>> _commandReader;
    private readonly ILogger _logger;
    private readonly int _tickInterval;
    

    public DownloadEventListener(IDownloadEventHandler handler, ChannelReader<Func<ICommandContext, Task>> commandReader, int tickInterval, ILogger logger)
    {
        _hander = handler;
        _commandReader = commandReader;
        _tickInterval = tickInterval;
        _logger = logger;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        var commandTask = _commandReader.ReadAsync(cancellationToken).AsTask();
        var intervalTask = Task.Delay(_tickInterval, cancellationToken);
        while (true)
        {
            Task ready = await Task.WhenAny(commandTask, intervalTask);
            if (ready == commandTask)
            {
                var command = await commandTask;
                try
                {
                    await command(_hander);
                }
                catch (Exception exc)
                {
                    _logger.LogError("handling user command", exc);
                }
                commandTask = _commandReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == intervalTask)
            {
                await _hander.OnTickAsync(cancellationToken);
                intervalTask = Task.Delay(_tickInterval, cancellationToken);
            }
        }
    }
}