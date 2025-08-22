using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;
public readonly record struct Block(PieceDownload Piece, int Begin, int Length)
{
    public static implicit operator PieceRequest(Block block) => new(block.Piece.PieceIndex, block.Begin, block.Length);
}