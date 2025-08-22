namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;
public class BlockStream : Stream
{
    private readonly PartsCursor _cursor;
    private readonly long _length;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position { set => throw new NotSupportedException(); get => throw new NotSupportedException(); }

    public BlockStream(PartsCursor parts, long length)
    {
        _cursor = parts;
        _length = length;
    }

    private int CapReadCount(int count)
    {
        return int.Min(count, _cursor.RemainingInPart);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!_cursor.TryGetPart(out var part)) return 0;
        var stream = await part.StreamData.Handle.Value;
        int readCount = await stream.ReadAsync(buffer[..CapReadCount(buffer.Length)], part.Position, cancellationToken);
        _cursor.Advance(readCount);
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        if (!_cursor.TryGetPart(out var part)) return 0;
        var stream = part.StreamData.Handle.Value.GetAwaiter().GetResult();
        int readCount = stream.Read(buffer[..CapReadCount(buffer.Length)], part.Position);
        _cursor.Advance(readCount);
        return readCount;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int writtenBytes = 0;
        while (writtenBytes < buffer.Length)
        {
            if (!_cursor.TryGetPart(out var part)) return;
            var handle = await part.StreamData.Handle.Value;
            int writeLen = CapReadCount(buffer.Length - writtenBytes);
            await handle.WriteAsync(buffer.Slice(writtenBytes, writeLen), part.Position, cancellationToken);
            _cursor.Advance(writeLen);
            writtenBytes += writeLen;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }
}
