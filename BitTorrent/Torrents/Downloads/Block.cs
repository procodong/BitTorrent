using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public readonly record struct Block(PieceDownload Piece, int Begin, int Length);