using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Helpers.Parsing;

namespace BitTorrentClient.Helpers.Streams;

public class FrameReader : IBufferReader
{
    private readonly BufferCursor _cursor;
    private readonly Stream _stream;
    private readonly int _size;
    private int _bytesRead;
    private int _remainingBuffered;
    
    public int RemainingUnbuffered => _size - _bytesRead;
    public int Buffered => _remainingBuffered;
    public int Remaining => RemainingUnbuffered + Buffered;
    public int AvailableBuffer => _cursor.AvailableBuffer;

    public FrameReader(Stream stream, BufferCursor cursor, int size)
    {
        _stream = stream;
        _cursor = cursor;
        _size = size;
        _bytesRead = int.Min(size, cursor.RemainingInitializedBytes);
        _remainingBuffered = _bytesRead;
    }
    
    public ReadOnlySpan<byte> GetSpan()
    {
        return _cursor.GetSpan()[.._remainingBuffered];
    }

    public ReadOnlyMemory<byte> GetMemory()
    {
        return _cursor.GetMemory()[.._remainingBuffered];
    }

    public Stream GetStream()
    {
        return new FrameStream(this);
    }

    public void Advance(int count)
    {
        _cursor.Advance(count);
        _remainingBuffered -= count;
    }

    public async Task<byte[]> ReadToEndAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[Remaining];
        var start = _bytesRead;
        while (RemainingUnbuffered != 0)
        {
            var buffered = GetMemory();
            buffered.CopyTo(buffer.AsMemory(_bytesRead - start));
            Advance(buffered.Length);
            await ReadAsync(cancellationToken);
        }
        return buffer;
    }

    public async Task EnsureReadAtLeastAsync(int count, CancellationToken cancellationToken = default)
    {
        var cappedCount = int.Min(count, _cursor.AvailableBuffer);
        while (Buffered < cappedCount)
        {
            await ReadAsync(cancellationToken);
        }
    }

    public async Task ReadAsync(CancellationToken cancellationToken = default)
    {
        if (RemainingUnbuffered == 0 || _cursor.RemainingBuffer == 0) return;
        var read = await _stream.ReadAsync(_cursor, cancellationToken);
        if (read == 0)
        {
            throw new EndOfStreamException();
        }
        _bytesRead = int.Min(_bytesRead + read, _size);
        _remainingBuffered = _bytesRead;
    }
}
