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
    private readonly ChannelReader<Stream> _requestReader;
    private readonly Stream _stream;
    private readonly ArrayBufferWriter<byte> _buffer;

    public PeerMessageWriter(Stream stream, ArrayBufferWriter<byte> buffer, PipeReader messageReader, ChannelReader<Stream> requestReader)
    {
        _messageReader = messageReader;
        _requestReader = requestReader;
        _stream = stream;
        _buffer = buffer;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<ReadResult> messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
        Task<Stream> pieceTask = _requestReader.ReadAsync(cancellationToken).AsTask();

        while (true)
        {
            var ready = await Task.WhenAny(messageTask, pieceTask);
            if (ready == messageTask)
            {
                var message = await messageTask;
                foreach (var chunk in message.Buffer)
                {
                    var buffer = _buffer.GetMemory(chunk.Length);
                    chunk.CopyTo(buffer);
                    _buffer.Advance(chunk.Length);
                }
                var data = _buffer.WrittenMemory;
                _buffer.ResetWrittenCount();
                await _stream.WriteAsync(data, cancellationToken);
                messageTask = _messageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (ready == pieceTask)
            {

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
