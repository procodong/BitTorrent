using System.Buffers.Binary;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public static class MessageEncoder
{
    public static void EncodePieceRequest(BigEndianBinaryWriter writer, BlockRequest request)
    {
        writer.Write(request.Index);
        writer.Write(request.Begin);
        writer.Write(request.Length);
    }

    public static void EncodePieceHeader(BigEndianBinaryWriter writer, BlockShareHeader block)
    {
        writer.Write(block.Index);
        writer.Write(block.Begin);
    }

    public static void EncodeHandShake(BigEndianBinaryWriter writer, HandShake handShake)
    {
        writer.Write((byte)handShake.Protocol.Length);
        writer.Write(handShake.Protocol);
        Span<byte> ext = stackalloc byte[8];
        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(ext, handShake.Extensions);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(ext, handShake.Extensions);
        }
        writer.Write(ext);
        writer.Write(handShake.InfoHash);
        writer.Write(handShake.PeerId);
    }

    public static void EncodeHeader(BigEndianBinaryWriter writer, MessageHeader header)
    {
        writer.Write(header.Length);
        writer.Write((byte)header.Type);
    }
}
