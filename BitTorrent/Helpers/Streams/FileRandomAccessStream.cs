using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Helpers.Streams;
internal class FileRandomAccessStream : IRandomAccesStream
{
    private readonly FileStream _stream;
    public FileRandomAccessStream(FileStream file)
    {
        _stream = file;
    }

    public int Read(Span<byte> buffer, long fileOffset)
    {
        return RandomAccess.Read(_stream.SafeFileHandle, buffer, fileOffset);
    }

    public ValueTask<int> ReadAsync(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
    {
        return RandomAccess.ReadAsync(_stream.SafeFileHandle, buffer, fileOffset, cancellationToken);
    }

    public void Write(ReadOnlySpan<byte> buffer, long fileOffset)
    {
        RandomAccess.Write(_stream.SafeFileHandle, buffer, fileOffset);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
    {
        return RandomAccess.WriteAsync(_stream.SafeFileHandle, buffer, fileOffset, cancellationToken);
    }
    public void Dispose()
    {
        _stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
}
