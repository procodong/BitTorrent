namespace BitTorrentClient.Helpers.Parsing;
public class BufferCursor
{
    public readonly byte[] Buffer;
    public int Position;
    public int End;

    public BufferCursor(byte[] buffer, int position = 0, int end = 0)
    {
        Buffer = buffer;
        Position = position;
        End = end;
    }
    public int RemainingInitializedBytes => End - Position;
    public int RemainingBuffer => Buffer.Length - Position;
}
