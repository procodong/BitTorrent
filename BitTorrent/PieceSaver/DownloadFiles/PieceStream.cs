using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using BitTorrent.Files.DownloadFiles;

namespace BitTorrent.PieceSaver.DownloadFiles;
public class PieceStream<S>(IEnumerable<StreamPart<S>> parts, int length) : Stream
    where S : Stream
{
    private readonly IEnumerable<StreamPart<S>> _parts = parts;
    private StreamPart<S> _currentpart = parts.First();
    private int _position = 0;
    private readonly int _length = length;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position { set => throw new NotSupportedException(); get => throw new NotSupportedException(); }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    private void Next()
    {
        var part = _parts.FirstOrDefault();
        if (part.StreamData.Stream is null)
        {
            return;
        }
        _currentpart = part;
        _position = 0;
    }

    private void UpdateCurrentFile()
    {
        if (_position >= _currentpart.Length)
        {
            Next();
        }
    }

    private int CapReadCount(int count)
    {
        return int.Min(count, _currentpart.Length - _position);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        UpdateCurrentFile();
        await _currentpart.StreamData.Lock.WaitAsync(cancellationToken);
        UpdatePosition();
        int readCount = await _currentpart.StreamData.Stream.ReadAsync(buffer[..CapReadCount(buffer.Length)], cancellationToken);
        FinalizeRead(readCount);
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        UpdateCurrentFile();
        _currentpart.StreamData.Lock.Wait();
        UpdatePosition();
        int readCount = _currentpart.StreamData.Stream.Read(buffer[..CapReadCount(buffer.Length)]);
        FinalizeRead(readCount);
        return readCount;
    }

    private void UpdatePosition()
    {
        _currentpart.StreamData.Stream.Position = _currentpart.Position + _position;
    }

    public void FinalizeRead(int readCount)
    {
        _currentpart.StreamData.Lock.Release();
        _position += readCount;
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
            UpdateCurrentFile();
            int writeLen = CapReadCount(buffer.Length - writtenBytes);
            await _currentpart.StreamData.Lock.WaitAsync(cancellationToken);
            UpdatePosition();
            await _currentpart.StreamData.Stream.WriteAsync(buffer.Slice(writtenBytes, writeLen), cancellationToken);
            FinalizeRead(writeLen);
            writtenBytes += writeLen;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }
}
