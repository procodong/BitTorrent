using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Engine.Infrastructure.MessageWriting;
using BitTorrentClient.Engine.Models.Messages;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Events.Listening;
public sealed class MessageWritingEventListener : IEventListener
{
    private readonly IMessageWritingEventHandler _handler;
    private readonly ChannelReader<MaybeRentedArray<Message>> _messageReader;
    private readonly ChannelReader<BlockRequest> _cancellationReader;
    private readonly PeriodicTimer _keepAliveTimer;

    public MessageWritingEventListener(IMessageWritingEventHandler handler, ChannelReader<MaybeRentedArray<Message>> messageReader, ChannelReader<BlockRequest> cancellationReader, PeriodicTimer keepAliveTimer)
    {
        _handler = handler;
        _messageReader = messageReader;
        _cancellationReader = cancellationReader;
        _keepAliveTimer = keepAliveTimer;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        var messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        var cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
        var delayTask = Task.Delay(-1, cancellationToken);
        var keepAliveTask = _keepAliveTimer.WaitForNextTickAsync(cancellationToken).AsTask();

        var delayer = new PieceDelayingHandle();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, cancelledBlockTask, delayTask, keepAliveTask);
            if (ready == messageTask)
            {
                var message = await messageTask;
                delayer.Changed = false;
                await _handler.OnMessageAsync(message, delayer, cancellationToken);
                if (delayer.Changed)
                {
                    delayTask = Task.Delay(delayer.DelayMilliSeconds, cancellationToken);
                }
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == cancelledBlockTask)
            {
                var cancel = await cancelledBlockTask;
                await _handler.OnCancelAsync(cancel, cancellationToken);
                cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == delayTask)
            {
                delayer.Reset();
                await _handler.OnDelayEnd(delayer, cancellationToken);
                delayTask = Task.Delay(delayer.DelayMilliSeconds, cancellationToken);
            }
            else if (ready == keepAliveTask)
            {
                await _handler.OnKeepAliveAsync(cancellationToken);
                keepAliveTask = _keepAliveTimer.WaitForNextTickAsync(cancellationToken).AsTask();
            }
        }
    }
}
