namespace BitTorrentClient.Helpers.Streams;
internal sealed class RandomAccessStream : IRandomAccesStream
{
    private readonly Stream _stream;
    private readonly SemaphoreSlim _lock;
    
    public RandomAccessStream(Stream stream)
    {
        _stream = stream;
        _lock = new(1, 1);
    }


    public int Read(Span<byte> buffer, long fileOffset)
    {
        var pos = _stream.Position;
        _stream.Position = fileOffset;
        _lock.Wait();
        int read;
        try
        {
            read = _stream.Read(buffer);
        }
        finally
        {
            _stream.Position = pos;
            _lock.Release();
        }
        return read;
    }

    public async ValueTask<int> ReadAsync(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
    {
        var pos = _stream.Position;
        _stream.Position = fileOffset;
        await _lock.WaitAsync(cancellationToken);
        int read;
        try
        {
            read = await _stream.ReadAsync(buffer, cancellationToken);
        }
        finally
        {
            _stream.Position = pos;
            _lock.Release();
        }
        return read;
    }

    public void Write(ReadOnlySpan<byte> buffer, long fileOffset)
    {
        var pos = _stream.Position;
        _stream.Position = fileOffset;
        _lock.WaitAsync();
        try
        {
            _stream.Write(buffer);
        }
        finally
        {
            _stream.Position = pos;
            _lock.Release();
        }
    }

    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
    {
        var pos = _stream.Position;
        _stream.Position = fileOffset;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(buffer, cancellationToken);
        }
        finally
        {
            _stream.Position = pos;
            _lock.Release();
        }
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
