using BitTorrentClient.Models.Messages;
using BitTorrentClient.Helpers.Parsing;
using System.Buffers.Binary;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
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

    public static BlockRequest DecodeRequest(BigEndianBinaryReader reader)
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
