using BitTorrentClient.Helpers.Parsing;

namespace BitTorrentClient.Helpers.Extensions;

public static class StreamExt
{
    private static async Task<int> ReadWithAsync(BufferCursor reader, Func<ValueTask<int>> read)
    {
        if (reader.Position != 0)
        {
            reader.Buffer.AsSpan(reader.Position..reader.End).CopyTo(reader.Buffer);
        }
        int readLen = await read();
        reader.End = reader.RemainingInitializedBytes + readLen;
        reader.Position = 0;
        if (readLen == 0)
        {
            throw new EndOfStreamException();
        }
        return readLen;
    }

    public static async Task<int> ReadAsync(this Stream stream, BufferCursor reader, CancellationToken cancellationToken = default)
    {
        return await ReadWithAsync(reader, () => stream.ReadAsync(reader.Buffer.AsMemory(reader.RemainingInitializedBytes), cancellationToken));
    }

    public static async Task<int> ReadAtLeastAsync(this Stream stream, BufferCursor reader, int minimumBytes, CancellationToken cancellationToken = default)
    {
        return await ReadWithAsync(reader, () => stream.ReadAtLeastAsync(reader.Buffer.AsMemory(reader.RemainingInitializedBytes), minimumBytes, cancellationToken: cancellationToken));
    }

}