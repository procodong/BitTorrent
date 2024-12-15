using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;

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
        get => throw new NotSupportedException(); 
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
                return read;
            case CurentStream.Second:
                return _secondStream.Read(buffer);
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
                return read;
            case CurentStream.Second:
                return await _secondStream.ReadAsync(buffer, cancellationToken);
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
