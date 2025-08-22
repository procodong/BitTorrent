using BitTorrentClient.Application.Infrastructure.Storage.Data;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;
internal class PieceDownload(int size, int pieceIndex, PieceHasher hasher)
{
    public int Downloaded;
    public int Index { get; }  = pieceIndex;
    public int Size { get; } = size;
    public PieceHasher Hasher { get; } = hasher;

    public static implicit operator Block(PieceDownload pieceDownload) => new(pieceDownload, 0, pieceDownload.Size);
}
