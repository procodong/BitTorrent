using BitTorrent.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents;
public static class MessageEncoder
{
    public static void EncodePieceRequest(Span<byte> buffer, PieceRequest request)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer, request.Index);
        BinaryPrimitives.WriteInt32BigEndian(buffer[4..], request.Begin);
        BinaryPrimitives.WriteInt32BigEndian(buffer[8..], request.Length);
    }

    public static void EncodePieceHeader(Span<byte> buffer, PieceShareHeader piece)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer, piece.Index);
        BinaryPrimitives.WriteInt32BigEndian(buffer[4..], piece.Begin);
    }

    public static void EncodeHandShake(Span<byte> buffer, HandShake handShake)
    {
        buffer[0] = (byte)handShake.Protocol.Length;
        Encoding.ASCII.GetBytes(handShake.Protocol).CopyTo(buffer[1..]);
        int offset = 1 + handShake.Protocol.Length;
        var infoHashIndex = offset + 8;
        for (int i = offset; i < infoHashIndex; i++)
        {
            buffer[i] = 0;
        }
        handShake.InfoHash.CopyTo(buffer[infoHashIndex..]);
        Encoding.ASCII.GetBytes(handShake.PeerId).CopyTo(buffer[(infoHashIndex + handShake.InfoHash.Length)..]);
    }

    public static void EncodeHeader(Span<byte> buffer, MessageHeader header)
    {
        BinaryPrimitives.WriteInt32BigEndian(buffer, header.Length);
        buffer[sizeof(int)] = (byte)header.Type;
    }
}
