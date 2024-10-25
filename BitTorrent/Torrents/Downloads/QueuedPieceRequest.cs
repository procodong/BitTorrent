using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public struct QueuedPieceRequest(PieceDownload download, int offset, int length)
{
    public PieceDownload Download = download;
    public readonly int Offset = offset;
    public readonly int Length = length;
}
