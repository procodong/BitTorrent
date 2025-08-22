namespace BitTorrentClient.Helpers.Extensions;

public static class StreamExt
{
    private static async Task<int> ReadWithAsync(Parsing.BufferCursor reader, Func<Memory<byte>, ValueTask<int>> read)
    {
        var buffer = reader.GetWriteMemory();
        int readLen = await read(buffer);
        if (readLen == 0)
        {
            throw new EndOfStreamException();
        }
        reader.AdvanceWritten(readLen);
        return readLen;
    }

    public static async Task<int> ReadAsync(this Stream stream, Parsing.BufferCursor reader, CancellationToken cancellationToken = default)
    {
        return await ReadWithAsync(reader, (b) => stream.ReadAsync(b, cancellationToken));
    }

    public static async Task<int> ReadAtLeastAsync(this Stream stream, Parsing.BufferCursor reader, int minimumBytes, CancellationToken cancellationToken = default)
    {
        return await ReadWithAsync(reader, (b) => stream.ReadAtLeastAsync(b, minimumBytes, cancellationToken: cancellationToken));
    }

}