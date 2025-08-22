using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;

namespace BitTorrentClient.Helpers.Streams;

public class FrameReader : IBufferReader
{
    private readonly BufferCursor _cursor;
    private readonly Stream _stream;
    private readonly int _size;
    private int _bytesRead;
    private int _bufferedEnd;
    
    public int RemainingUnbuffered => _size - _bytesRead;
    public int Buffered => _bufferedEnd - _cursor.Position;
    public int Remaining => RemainingUnbuffered + Buffered;

    public FrameReader(Stream stream, BufferCursor cursor, int size)
    {
        _stream = stream;
        _cursor = cursor;
        _size = size;
        _bytesRead = int.Min(size, cursor.RemainingInitializedBytes);
        _bufferedEnd = cursor.Position + _bytesRead;
    }
    
    public ReadOnlySpan<byte> GetSpan()
    {
        return _cursor.Buffer.AsSpan(_cursor.Position.._bufferedEnd);
    }

    public ReadOnlyMemory<byte> GetMemory()
    {
        return _cursor.Buffer.AsMemory(_cursor.Position.._bufferedEnd);
    }

    public Stream GetStream()
    {
        return new FrameStream(this);
    }

    public void Advance(int count)
    {
        _cursor.Position += count;
    }

    public async Task<byte[]> ReadToEndAsync(CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[Remaining];
        int start = _bytesRead;
        while (RemainingUnbuffered != 0)
        {
            var buffered = GetMemory();
            buffered.CopyTo(buffer.AsMemory(_bytesRead - start));
            Advance(buffered.Length);
            await ReadAsync(cancellationToken);
        }
        return buffer;
    }

    public async Task EnsureReadAtleastAsync(int count, CancellationToken cancellationToken = default)
    {
        int cappedCount = int.Min(count, _cursor.Buffer.Length - _cursor.RemainingInitializedBytes);
        while (Buffered < cappedCount)
        {
            await ReadAsync(cancellationToken);
        }
    }

    public async Task ReadAsync(CancellationToken cancellationToken = default)
    {
        if (RemainingUnbuffered == 0 || _cursor.RemainingBuffer == 0) return;
        int read = await _stream.ReadAsync(_cursor, cancellationToken);
        if (read == 0)
        {
            throw new EndOfStreamException();
        }
        _bufferedEnd = int.Min(_cursor.End, RemainingUnbuffered);
        _bytesRead = int.Min(_bytesRead + read, _size);
    }
}
