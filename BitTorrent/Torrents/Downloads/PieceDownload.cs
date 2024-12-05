using BitTorrent.PieceSaver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class PieceDownload(int size, int pieceIndex, PieceHasher hasher, int offset = 0)
{
    public int Downloading = 0;
    public int Downloaded = 0;
    public int DownloadOffset = offset;
    public readonly int PieceIndex = pieceIndex;
    public readonly int Size = size;
    public readonly PieceHasher Hasher = hasher;
}