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
        var taskListener = new TaskListener<EventType>(cancellationToken);
        taskListener.AddTask(EventType.Message, () => _messageReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.CancelledBlock, () => _cancellationReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.KeepAlive, () => _keepAliveTimer.WaitForNextTickAsync(cancellationToken).AsTask());

        var delayer = new DelaySchedulingHandle(delay => taskListener.AddTask(EventType.Delay, Task.Delay(delay, cancellationToken)));
        while (true)
        {
            var (eventType, readyTask) = await taskListener.WaitAsync();

            switch (eventType)
            {
                case EventType.Message:
                {
                    using var message = await (Task<MaybeRentedArray<Message>>)readyTask;
                    await _handler.OnMessageAsync(message, delayer, cancellationToken);
                    break;
                }
                case EventType.CancelledBlock:
                {
                    var cancel = await (Task<BlockRequest>)readyTask;
                    await _handler.OnCancelAsync(cancel, cancellationToken);
                    break;
                }
                case EventType.Delay:
                    await _handler.OnDelayEnd(delayer, cancellationToken);
                    break;
                case EventType.KeepAlive:
                    await _handler.OnKeepAliveAsync(cancellationToken);
                    break;

            }
        }
    }
    enum EventType
    {
        Message,
        CancelledBlock,
        Delay,
        KeepAlive
    }
}