namespace BitTorrentClient.Helpers.Streams;

public sealed class FrameStream : Stream
{
    private readonly FrameReader _reader;
    private readonly int _length;

    public FrameStream(FrameReader reader)
    {
        _reader = reader;
        _length = reader.Remaining;
    }
    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead
    {
        get => true;
    }
    public override bool CanSeek
    {
        get => false;
    }
    public override bool CanWrite
    {
        get => false;
    }
    public override long Length
    {
        get => _length;
    }
    public override long Position 
    { 
        get => throw new NotSupportedException(); 
        set => throw new NotSupportedException(); 
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_reader.Remaining == 0) return 0;
        var buffered = _reader.GetMemory();
        if (buffered.Length == 0)
        {
            await _reader.ReadAsync(cancellationToken);
            buffered = _reader.GetMemory();
        }
        var read = int.Min(buffered.Length, buffer.Length);
        buffered[..read].CopyTo(buffer);
        _reader.Advance(read);
        return read;
    }
}