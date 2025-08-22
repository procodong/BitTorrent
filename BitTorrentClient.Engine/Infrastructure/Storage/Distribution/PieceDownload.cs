using BitTorrentClient.Engine.Infrastructure.Storage.Data;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
public class PieceDownload(int size, int pieceIndex, PieceHasher hasher)
{
    public int Downloaded;
    public int Index { get; }  = pieceIndex;
    public int Size { get; } = size;
    public PieceHasher Hasher { get; } = hasher;

    public static implicit operator Block(PieceDownload pieceDownload) => new(pieceDownload, 0, pieceDownload.Size);
}
