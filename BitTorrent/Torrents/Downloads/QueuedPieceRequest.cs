using BitTorrent.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public readonly record struct QueuedPieceRequest(PieceDownload Download, PieceRequest Request);