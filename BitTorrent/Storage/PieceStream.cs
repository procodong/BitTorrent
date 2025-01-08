using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage;
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
    public StreamPart CurrentPart => _parts.Current;

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
        if (_position >= CurrentPart.Length)
        {
            return Next();
        }
        return true;
    }

    private int CapReadCount(int count)
    {
        return int.Min(count, CurrentPart.Length - _position);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!UpdateCurrentFile()) return 0;
        var handle = await CurrentPart.StreamData.Handle.Value;
        await handle.Lock.WaitAsync(cancellationToken);
        UpdatePosition(handle.Stream);
        int readCount;
        try
        {
            readCount = await handle.Stream.ReadAsync(buffer[..CapReadCount(buffer.Length)], cancellationToken);
        }
        finally
        {
            handle.Lock.Release();
        }
        _position += readCount;
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        if (!UpdateCurrentFile()) return 0;
        var handle = CurrentPart.StreamData.Handle.Value.GetAwaiter().GetResult();
        handle.Lock.Wait();
        UpdatePosition(handle.Stream);
        int readCount;
        try
        {
            readCount = handle.Stream.Read(buffer[..CapReadCount(buffer.Length)]);
        }
        finally
        {
            handle.Lock.Release();
        }
        _position += readCount;
        return readCount;
    }

    private void UpdatePosition(Stream stream)
    {
        stream.Position = CurrentPart.Position + _position;
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
        WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int writtenBytes = 0;
        while (writtenBytes < buffer.Length)
        {
            if (!UpdateCurrentFile()) return;
            var handle = await CurrentPart.StreamData.Handle.Value;
            int writeLen = CapReadCount(buffer.Length - writtenBytes);
            await handle.Lock.WaitAsync(cancellationToken);
            UpdatePosition(handle.Stream);
            try
            {
                await handle.Stream.WriteAsync(buffer.Slice(writtenBytes, writeLen), cancellationToken);
            }
            finally
            {
                handle.Lock.Release();
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
