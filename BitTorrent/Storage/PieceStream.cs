using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Storage;
public class PieceStream : Stream
{
    private readonly IEnumerator<StreamPart> _parts;
    private int _position = 0;
    private readonly int _length;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position { set => throw new NotSupportedException(); get => throw new NotSupportedException(); }
    public StreamPart Current => _parts.Current;

    public PieceStream(IEnumerable<StreamPart> parts, int length)
    {
        _parts = parts.GetEnumerator();
        _length = length;
        _parts.MoveNext();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    private bool Next()
    {
        _position = 0;
        return _parts.MoveNext();
    }

    private bool UpdateCurrentFile()
    {
        if (_position >= Current.Length)
        {
            return Next();
        }
        return true;
    }

    private int CapReadCount(int count)
    {
        return int.Min(count, Current.Length - _position);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!UpdateCurrentFile()) return 0;
        await Current.StreamData.Lock.WaitAsync(cancellationToken);
        UpdatePosition();
        int readCount = -1;
        try
        {
            readCount = await Current.StreamData.Stream.ReadAsync(buffer[..CapReadCount(buffer.Length)], cancellationToken);
        }
        finally
        {
            FinalizeRead(readCount);
        }
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        if (!UpdateCurrentFile()) return 0;
        Current.StreamData.Lock.Wait();
        UpdatePosition();
        int readCount = -1;
        try
        {
            readCount = Current.StreamData.Stream.Read(buffer[..CapReadCount(buffer.Length)]);
        }
        finally
        {
            FinalizeRead(readCount);
        }
        return readCount;
    }

    private void UpdatePosition()
    {
        Current.StreamData.Stream.Position = Current.Position + _position;
    }

    public void FinalizeRead(int readCount)
    {
        Current.StreamData.Lock.Release();
        if (readCount != -1)
        {
            _position += readCount;
        }
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
        WriteAsync(buffer.AsMemory(offset, count)).GetAwaiter().GetResult();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int writtenBytes = 0;
        while (writtenBytes < buffer.Length)
        {
            if (!UpdateCurrentFile()) return;
            int writeLen = CapReadCount(buffer.Length - writtenBytes);
            await Current.StreamData.Lock.WaitAsync(cancellationToken);
            UpdatePosition();
            try
            {
                await Current.StreamData.Stream.WriteAsync(buffer.Slice(writtenBytes, writeLen), cancellationToken);
            }
            finally
            {
                Current.StreamData.Lock.Release();
            }
            _position += writeLen;
            writtenBytes += writeLen;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }
}
