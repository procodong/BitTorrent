using System.Threading.Channels;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using Microsoft.Testing.Platform.Logging;

namespace BitTorrentClient.Application.Events.Listening.Downloads;

public class DownloadEventListener : IEventListener
{
    private readonly IDownloadEventHandler _hander;
    private readonly IPeerReceiver _peerReceiver;
    private readonly ChannelReader<Func<ICommandContext, Task>> _commandReader;
    private readonly ILogger _logger;
    private readonly int _tickInterval;


    public DownloadEventListener(IDownloadEventHandler handler, IPeerReceiver peerReceiver, ChannelReader<Func<ICommandContext, Task>> commandReader, int tickInterval, ILogger logger)
    {
        _hander = handler;
        _commandReader = commandReader;
        _tickInterval = tickInterval;
        _logger = logger;
        _peerReceiver = peerReceiver;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        var commandTask = _commandReader.ReadAsync(cancellationToken).AsTask();
        var intervalTask = Task.Delay(_tickInterval, cancellationToken);
        var peerTask = _peerReceiver.ReceivePeerAsync(cancellationToken);
        while (true)
        {
            Task ready = await Task.WhenAny(commandTask, intervalTask, peerTask);
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
            else if (ready == peerTask)
            {
                var peer = await peerTask;
                await _hander.OnPeerAsync(peer, cancellationToken);
                peerTask = _peerReceiver.ReceivePeerAsync(cancellationToken);
            }
        }
    }
}