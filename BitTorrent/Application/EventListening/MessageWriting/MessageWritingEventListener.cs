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
    private readonly PipeReader _messageReader;
    private readonly ChannelReader<BlockData> _requestReader;
    private readonly ChannelReader<PieceRequest> _cancellationReader;

    public MessageWritingEventListener(IMessageWritingEventHandler handler, PipeReader messageReader, ChannelReader<BlockData> requestReader, ChannelReader<PieceRequest> cancellationReader)
    {
        _handler = handler;
        _messageReader = messageReader;
        _requestReader = requestReader;
        _cancellationReader = cancellationReader;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<ReadResult> messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        Task<BlockData> pieceTask = _requestReader.ReadAsync(cancellationToken).AsTask();
        Task<PieceRequest> cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, pieceTask, cancelledBlockTask);
            if (ready == messageTask)
            {
                var message = await messageTask;
                await _handler.OnMessageAsync(message.Buffer, cancellationToken);
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == pieceTask)
            {
                var data = await pieceTask;
                await _handler.OnBlockAsync(data, cancellationToken);
                pieceTask = _requestReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == cancelledBlockTask)
            {
                var block = await cancelledBlockTask;
                await _handler.OnCancelAsync(block, cancellationToken);
                cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
            }
        }
    }
}
