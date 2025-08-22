using BitTorrentClient.Models.Messages;
using BitTorrentClient.Helpers.Parsing;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public static class MessageEncoder
{
    public static void EncodePieceRequest(BigEndianBinaryWriter writer, PieceRequest request)
    {
        writer.Write(request.Index);
        writer.Write(request.Begin);
        writer.Write(request.Length);
    }

    public static void EncodePieceHeader(BigEndianBinaryWriter writer, PieceShareHeader piece)
    {
        writer.Write(piece.Index);
        writer.Write(piece.Begin);
    }

    public static void EncodeHandShake(BigEndianBinaryWriter writer, HandShake handShake)
    {
        writer.Write((byte)handShake.Protocol.Length);
        writer.Write(handShake.Protocol);
        writer.Write(stackalloc byte[8]);
        writer.Write(handShake.InfoHash);
        writer.Write(handShake.PeerId);
    }

    public static void EncodeHeader(BigEndianBinaryWriter writer, MessageHeader header)
    {
        writer.Write(header.Length);
        writer.Write((byte)header.Type);
    }
}
