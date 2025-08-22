using BitTorrentClient.Models.Messages;
using System.Buffers;
using System.Threading.Channels;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;
public class MessageWritingEventListener : IEventListener
{
    private readonly IMessageWritingEventHandler _handler;
    private readonly ChannelReader<IMemoryOwner<Message>> _messageReader;
    private readonly ChannelReader<BlockRequest> _cancellationReader;

    public MessageWritingEventListener(IMessageWritingEventHandler handler, ChannelReader<IMemoryOwner<Message>> messageReader, ChannelReader<BlockRequest> cancellationReader)
    {
        _handler = handler;
        _messageReader = messageReader;
        _cancellationReader = cancellationReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<IMemoryOwner<Message>> messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        Task<BlockRequest> cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
        Task delayTask = Task.Delay(-1, cancellationToken);

        var delayer = new PieceDelayingHandle();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, cancelledBlockTask, delayTask);
            if (ready == messageTask)
            {
                var message = await messageTask;
                delayer.Changed = false;
                await _handler.OnMessageAsync(message, delayer, cancellationToken);
                if (delayer.Changed)
                {
                    delayTask = Task.Delay(delayer.Delay, cancellationToken);
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
                delayTask = Task.Delay(delayer.Delay, cancellationToken);
            }
        }
    }
}
