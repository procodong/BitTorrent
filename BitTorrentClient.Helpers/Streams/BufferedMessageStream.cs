using System.Buffers.Binary;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;

namespace BitTorrentClient.Helpers.Streams;

public class BufferedMessageStream : IDisposable, IAsyncDisposable
{
    private readonly BufferCursor _cursor;
    private readonly Stream _stream;

    public BufferedMessageStream(Stream reader, BufferCursor cursor)
    {
        _stream = reader;
        _cursor = cursor;
    }

    public async Task<FrameReader> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        while (_cursor.RemainingInitializedBytes < 4)
        {
            await _stream.ReadAsync(_cursor, cancellationToken);
        }
        int size = BinaryPrimitives.ReadInt32BigEndian(_cursor.GetSpan());
        _cursor.Advance(sizeof(int));
        return new(_stream, _cursor, size);
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