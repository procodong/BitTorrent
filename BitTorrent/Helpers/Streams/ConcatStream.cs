namespace BitTorrentClient.Helpers.Streams;

enum CurentStream
{
    First,
    Second
}

public class ConcatStream : Stream
{
    private readonly Stream _firstStream;
    private readonly Stream _secondStream;
    private CurentStream _current = CurentStream.First;
    private int _positon;

    public ConcatStream(Stream firstStream, Stream secondStream)
    {
        _firstStream = firstStream;
        _secondStream = secondStream;
    }

    public override bool CanRead => _firstStream.CanRead && _secondStream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _firstStream.Length + _secondStream.Length;

    public override long Position {
        get => _positon; 
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new InvalidOperationException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        switch (_current)
        {
            case CurentStream.First:
                int read = _firstStream.Read(buffer);
                if (read == 0)
                {
                    _current = CurentStream.Second;
                    return _secondStream.Read(buffer);
                }
                _positon += read;
                return read;
            case CurentStream.Second:
                int count = _secondStream.Read(buffer);
                _positon += count;
                return count;
        }
        return 0;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        switch (_current)
        {
            case CurentStream.First:
                int read = await _firstStream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    _current = CurentStream.Second;
                    read = await _secondStream.ReadAsync(buffer, cancellationToken);
                }
                _positon += read;
                return read;
            case CurentStream.Second:
                int count = await _secondStream.ReadAsync(buffer, cancellationToken);
                _positon += count;
                return count;
            default:
                throw new InvalidDataException();
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new InvalidOperationException();
    }

    public override void SetLength(long value)
    {
        throw new InvalidOperationException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException();
    }
}
