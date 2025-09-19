namespace BitTorrentClient.Helpers.Parsing;
public sealed class BufferCursor : IBufferReader
{
    private readonly byte[] _buffer;
    private int _end;
    private int _position;

    public BufferCursor(byte[] buffer, int end = 0)
    {
        _buffer = buffer;
        _end = end;
    }

    public BufferCursor(int len) : this(new byte[len])
    {

    }

    public int RemainingInitializedBytes => _end - _position;
    public int AvailableBuffer => RemainingInitializedBytes - _buffer.Length;
    public int RemainingBuffer => _buffer.Length - RemainingInitializedBytes;

    public void Advance(int count)
    {
        _position += count;
    }

    public ReadOnlyMemory<byte> GetMemory()
    {
        return _buffer.AsMemory(_position.._end);
    }

    public ReadOnlySpan<byte> GetSpan()
    {
        return _buffer.AsSpan(_position.._end);
    }

    public void AdvanceWritten(int count)
    {
        _end += count;
    }

    private void PrepareForWrite()
    {
        if (_position != 0)
        {
            _buffer.AsSpan(_position.._end).CopyTo(_buffer);
            _position = 0;
        }
    }

    public Memory<byte> GetWriteMemory()
    {
        PrepareForWrite();
        return _buffer.AsMemory(_end..);
    }

    public Span<byte> GetWriteSpan()
    {
        PrepareForWrite();
        return _buffer.AsSpan(_end..);
    }
}
