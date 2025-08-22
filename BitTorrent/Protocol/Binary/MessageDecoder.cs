using BitTorrentClient.Models.Messages;
using BitTorrentClient.Helpers.Parsing;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Binary;
public static class MessageDecoder
{
    public const int HandshakeLen = 49 + 19;
    public static HandShake DecodeHandShake(BigEndianBinaryReader reader)
    {
        var protocol = reader.ReadString();
        var extensions = BinaryPrimitives.ReadUInt64LittleEndian(reader.ReadBytes(8));
        var infoHash = reader.ReadBytes(20);
        var peerId = reader.ReadBytes(20);
        return new(protocol, extensions, infoHash.ToArray(), peerId.ToArray());
    }

    public static PieceRequest DecodeRequest(BigEndianBinaryReader reader)
    {
        var index = reader.ReadInt32();
        var begin = reader.ReadInt32();
        var length = reader.ReadInt32();
        return new(index, begin, length);
    }

    public static PieceShareHeader DecodePieceHeader(BigEndianBinaryReader reader)
    {
        var index = reader.ReadInt32();
        var begin = reader.ReadInt32();
        return new(index, begin);
    }
}
