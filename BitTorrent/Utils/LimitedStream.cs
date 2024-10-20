using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public class LimitedStream : Stream
{
    private readonly Stream _stream;
    private readonly long _length;
    private long _position = 0;

    internal LimitedStream(Stream stream, int length)
    {
        _stream = stream;
        _length = length;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position 
    { 
        get => _position;  
        set
        {
            if (value > _length)
            {
                throw new ArgumentOutOfRangeException("Position", message: "Position can't be out of the range of the stream");
            }
            _position = (int) value;
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    private int CapReadCount(int count)
    {
        return (int) long.Min(_length - _position, count);
    }

    public override int Read(Span<byte> buffer)
    {
        int read = _stream.Read(buffer[..CapReadCount(buffer.Length)]);
        _position += read;
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var readLen = await _stream.ReadAsync(buffer[..CapReadCount(buffer.Length)], cancellationToken);
        _position += readLen;
        return readLen;
    }

    public override bool CanTimeout => _stream.CanTimeout;

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length + offset;
                break;
        }
        return Position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }
}
