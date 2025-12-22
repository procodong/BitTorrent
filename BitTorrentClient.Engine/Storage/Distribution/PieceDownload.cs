using BitTorrentClient.Engine.Storage.Data;

namespace BitTorrentClient.Engine.Storage.Distribution;
public sealed class PieceDownload
{
    public int Downloaded;
    public int Index { get; }
    public int Size { get; }
    public Lock HashingLock { get; } = new();
    public PieceHasher Hasher { get; }

    public PieceDownload(int size, int pieceIndex, PieceHasher hasher)
    {
        Size = size;
        Index = pieceIndex;
        Hasher = hasher;
    }

    public static implicit operator Block(PieceDownload pieceDownload) => new(pieceDownload, 0, pieceDownload.Size);
}
