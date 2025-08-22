using BitTorrentClient.Models.Messages;
using System.Buffers;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;
public class MessageWritingEventListener : IEventListener
{
    private readonly IMessageWritingEventHandler _handler;
    private readonly ChannelReader<IMemoryOwner<Message>> _messageReader;
    private readonly ChannelReader<PieceRequest> _cancellationReader;

    public MessageWritingEventListener(IMessageWritingEventHandler handler, ChannelReader<IMemoryOwner<Message>> messageReader, ChannelReader<PieceRequest> cancellationReader)
    {
        _handler = handler;
        _messageReader = messageReader;
        _cancellationReader = cancellationReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<IMemoryOwner<Message>> messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        Task<PieceRequest> cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
        Task delayTask = Task.Delay(-1, cancellationToken);

        var delayer = new PieceDelayingHandle();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, cancelledBlockTask, delayTask);
            if (ready == messageTask)
            {
                // FIX ME
                var message = await messageTask;
                delayer.Reset();
                await _handler.OnMessageAsync(message, delayer, cancellationToken);
                delayTask = Task.Delay(delayer.Delay, cancellationToken);
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == cancelledBlockTask)
            {
                var block = await cancelledBlockTask;
                await _handler.OnCancelAsync(block, cancellationToken);
                cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == delayTask)
            {
                await _handler.OnDelayEnd(cancellationToken);
                delayTask = Task.Delay(-1, cancellationToken);
            }
        }
    }
}
