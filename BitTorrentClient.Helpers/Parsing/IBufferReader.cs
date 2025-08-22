namespace BitTorrentClient.Helpers.Parsing;

public interface IBufferReader
{
    ReadOnlySpan<byte> GetSpan();
    ReadOnlyMemory<byte> GetMemory();
    void Advance(int count);
}