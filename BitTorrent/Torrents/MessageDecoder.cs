using BitTorrent.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents;
public static class MessageDecoder
{
    public static HandShake DecodeHandShake(ReadOnlySpan<byte> data)
    {
        const int PEER_ID_SIZE = 20;
        var protocolLen = data[0];
        var protocol = Encoding.ASCII.GetString(data[1..(1 + protocolLen)]);
        var infoHashIndex = 1 + protocol.Length + 8;
        var peerIdIndex = infoHashIndex + SHA1.HashSizeInBytes;
        var infoHash = data[infoHashIndex..peerIdIndex];
        var peerId = Encoding.ASCII.GetString(data[peerIdIndex..(peerIdIndex + PEER_ID_SIZE)]);
        return new(protocol, infoHash.ToArray(), peerId);
    }

    public static PieceRequest DecodeRequest(ReadOnlySpan<byte> data)
    {
        var index = BinaryPrimitives.ReadInt32BigEndian(data);
        var begin = BinaryPrimitives.ReadInt32BigEndian(data[sizeof(int)..]);
        var length = BinaryPrimitives.ReadInt32BigEndian(data[(2 * sizeof(int))..]);
        return new(index, begin, length);
    }

    public static PieceShare DecodePiece(ReadOnlyMemory<byte> data)
    {
        var index = BinaryPrimitives.ReadInt32BigEndian(data.Span);
        var begin = BinaryPrimitives.ReadInt32BigEndian(data.Span[sizeof(int)..]);
        var block = data[(2 * sizeof(int))..];
        return new(index, begin, block);
    }
}
