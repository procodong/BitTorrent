using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class PieceDownload(int downloaded, int downloading, int size, int pieceIndex)
{
    public volatile int Downloading = downloading;
    public volatile int Downloaded = downloaded;
    public readonly int PieceIndex = pieceIndex;
    public readonly int Size = size;
}