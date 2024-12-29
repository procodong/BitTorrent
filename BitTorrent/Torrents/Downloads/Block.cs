using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Torrents.Downloads;
public readonly record struct Block(PieceDownload Piece, int Begin, int Length)
{
    public static implicit operator PieceRequest(Block block) => new(block.Piece.PieceIndex, block.Begin, block.Length);
}