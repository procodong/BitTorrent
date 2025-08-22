using BitTorrentClient.BitTorrent.Peers.Parsing;
using BitTorrentClient.Models.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers.Connections;
public class PeerMessageWriter : IAsyncDisposable, IDisposable
{
    private readonly PipeReader _messageReader;
    private readonly ChannelReader<BlockData> _requestReader;
    private readonly ChannelReader<PieceRequest> _cancellationReader;
    private readonly PipeWriter _output;

    public PeerMessageWriter(PipeWriter output, PipeReader messageReader, ChannelReader<BlockData> requestReader, ChannelReader<PieceRequest> cancellationReader)
    {
        _messageReader = messageReader;
        _requestReader = requestReader;
        _cancellationReader = cancellationReader;
        _output = output;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<ReadResult> messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        Task<BlockData> pieceTask = _requestReader.ReadAsync(cancellationToken).AsTask();
        Task<PieceRequest> cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, pieceTask);
            if (ready == messageTask)
            {
                var message = await messageTask;
                foreach (var chunk in message.Buffer)
                {
                    _output.Write(chunk.Span);
                }
                await _output.FlushAsync(cancellationToken);
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == pieceTask)
            {
                var data = await pieceTask;
                var writer = new MessageWriter(_output);
                writer.WritePieceHeader(data.Request);
                await data.Stream.CopyToAsync(_output, cancellationToken);
                await _output.FlushAsync(cancellationToken);
                pieceTask = _requestReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == cancelledBlockTask)
            {
                var block = await cancelledBlockTask;
                cancelledBlockTask = _cancellationReader.ReadAsync(cancellationToken).AsTask();
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return _messageReader.CompleteAsync();
    }

    public void Dispose()
    {
        _messageReader.Complete();
    }
}
