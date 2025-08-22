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
        var stream = await CurrentPart.StreamData.Handle.Value;
        int readCount = await stream.ReadAsync(buffer[..CapReadCount(buffer.Length)], CurrentPart.Position, cancellationToken);
        _position += readCount;
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        if (!UpdateCurrentFile()) return 0;
        var handle = CurrentPart.StreamData.Handle.Value.GetAwaiter().GetResult();
        int readCount = handle.Read(buffer[..CapReadCount(buffer.Length)], CurrentPart.Position);
        _position += readCount;
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
            if (!UpdateCurrentFile()) return;
            var handle = await CurrentPart.StreamData.Handle.Value;
            int writeLen = CapReadCount(buffer.Length - writtenBytes);
            await handle.WriteAsync(buffer.Slice(writtenBytes, writeLen), CurrentPart.Position, cancellationToken);
            _position += writeLen;
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
