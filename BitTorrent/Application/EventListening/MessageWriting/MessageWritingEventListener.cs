using BitTorrentClient.Models.Messages;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventListening.MessageWriting;
public class MessageWritingEventListener
{
    private readonly IMessageWritingEventHandler _handler;
    private readonly ChannelReader<ReadOnlyMemory<Message>> _messageReader;
    private readonly ChannelReader<BlockData> _blockReader;
    private readonly ChannelReader<PieceRequest> _cancellationReader;

    public MessageWritingEventListener(IMessageWritingEventHandler handler, ChannelReader<ReadOnlyMemory<Message>> messageReader, ChannelReader<BlockData> blockReader, ChannelReader<PieceRequest> cancellationReader)
    {
        _handler = handler;
        _messageReader = messageReader;
        _cancellationReader = cancellationReader;
        _blockReader = blockReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<ReadOnlyMemory<Message>> messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        Task<PieceRequest> cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
        Task<BlockData> blockTask = _blockReader.ReadAsync(cancellationToken).AsTask();

        var delayer = new PieceDelayingHandle();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, cancelledBlockTask, blockTask);
            if (ready == messageTask)
            {
                var message = await messageTask;
                await _handler.OnMessageAsync(message, cancellationToken);
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == cancelledBlockTask)
            {
                var block = await cancelledBlockTask;
                await _handler.OnCancelAsync(block, cancellationToken);
                cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == blockTask)
            {
                var block = await blockTask;
                await _handler.OnBlockAsync(block, delayer, cancellationToken);
                blockTask = DelayedTask(_blockReader.ReadAsync(cancellationToken).AsTask(), delayer.Delay, cancellationToken);
            }
        }
    }

    private async Task<T> DelayedTask<T>(Task<T> task, int millisecondsDelay, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(_blockReader.ReadAsync(cancellationToken).AsTask(), Task.Delay(millisecondsDelay, cancellationToken));
        return await task;
    }
}
