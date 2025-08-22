namespace BitTorrentClient.Helpers.Parsing;

public class ArrayBufferReader : IBufferReader
{
    private readonly BufferCursor _cursor;

    public ArrayBufferReader(BufferCursor cursor)
    {
        _cursor = cursor;
    }

    public ReadOnlySpan<byte> GetSpan()
    {
        return _cursor.Buffer.AsSpan(_cursor.Position.._cursor.End);
    }

    public ReadOnlyMemory<byte> GetMemory()
    {
        return _cursor.Buffer.AsMemory(_cursor.Position.._cursor.End);
    }

    public void Advance(int count)
    {
        _cursor.Position += count;
    }
}