using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
internal readonly record struct Block(PieceDownload Piece, int Begin, int Length)
{
    public static implicit operator BlockRequest(Block block) => new(block.Piece.Index, block.Begin, block.Length);
}