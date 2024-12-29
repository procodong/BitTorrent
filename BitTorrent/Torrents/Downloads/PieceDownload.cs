using BitTorrentClient.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Torrents.Downloads;
public class PieceDownload(int size, int pieceIndex, PieceHasher hasher)
{
    public int Downloaded;
    public readonly int PieceIndex = pieceIndex;
    public readonly int Size = size;
    public readonly PieceHasher Hasher = hasher;
}