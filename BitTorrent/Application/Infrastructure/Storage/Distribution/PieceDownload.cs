using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;
public class PieceDownload(int size, int pieceIndex, byte[] buffer)
{
    public int Downloaded;
    public readonly int PieceIndex = pieceIndex;
    public readonly int Size = size;
    public readonly byte[] Buffer = buffer;

    public static implicit operator Block(PieceDownload pieceDownload) => new(pieceDownload, 0, pieceDownload.Size);
}