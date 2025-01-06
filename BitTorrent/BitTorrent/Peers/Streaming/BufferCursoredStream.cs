using BitTorrentClient.Utils.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers.Streaming;
public class BufferCursoredStream : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;

    public BufferCursoredStream(Stream stream)
    {
        _stream = stream;
    }

    public Stream UnderlyingStream => _stream;

    private static async Task<int> ReadWithAsync(BufferCursor reader, Func<ValueTask<int>> read)
    {
        reader.Buffer.AsSpan(reader.Position..reader.Length).CopyTo(reader.Buffer);
        int readLen = await read();
        reader.Length = reader.RemainingBytes + readLen;
        reader.Position = 0;
        if (readLen == 0)
        {
            throw new EndOfStreamException();
        }
        return readLen;
    }

    public async Task<int> ReadAsync(BufferCursor reader, CancellationToken cancellationToken = default)
    {
        return await ReadWithAsync(reader, () => _stream.ReadAsync(reader.Buffer.AsMemory(reader.RemainingBytes), cancellationToken));
    }

    public async Task<int> ReadAtLeastAsync(BufferCursor reader, int minimumBytes, CancellationToken cancellationToken = default)
    {
        return await ReadWithAsync(reader, () => _stream.ReadAtLeastAsync(reader.Buffer.AsMemory(reader.RemainingBytes), minimumBytes, cancellationToken: cancellationToken));
    }

    public async Task WriteAsync(BufferCursor writer, CancellationToken cancellationToken = default)
    {
        await _stream.WriteAsync(writer.Buffer.AsMemory(..writer.Position), cancellationToken);
        writer.Position = 0;
    }

    public async Task FlushAsync(BufferCursor writer, CancellationToken cancellationToken = default)
    {
        await WriteAsync(writer, cancellationToken);
        await _stream.FlushAsync(cancellationToken);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
}
