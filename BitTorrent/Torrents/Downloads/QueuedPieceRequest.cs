using BitTorrent.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public struct QueuedPieceRequest(PieceDownload download, PieceRequest request)
{
    public PieceDownload Download = download;
    public readonly PieceRequest Request = request;
}
