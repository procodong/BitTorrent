namespace BitTorrentClient.Helpers.Streams;
public interface IRandomAccesStream : IDisposable, IAsyncDisposable
{
    int Read(Span<byte> buffer, long fileOffset);
    void Write(ReadOnlySpan<byte> buffer, long fileOffset);
    ValueTask<int> ReadAsync(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default);
}
