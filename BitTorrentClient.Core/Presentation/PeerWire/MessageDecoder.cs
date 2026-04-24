using System.Buffers.Binary;
using BitTorrentClient.Core.Presentation.PeerWire.Models;
using System.Buffers;
using BitTorrentClient.Helpers.Extensions;

namespace BitTorrentClient.Core.Presentation.PeerWire;
public static class MessageDecoder
{
    public const int HandshakeLen = 49 + 19;
    public const int HeaderLen = 5;

    public static int GetExpectedMessageLength(MessageType type, int bitfieldSize)
    {
        return type switch
        {
            MessageType.Choke or MessageType.UnChoke or MessageType.Interested or MessageType.NotInterested => 1,
            MessageType.Have => 5,
            MessageType.Bitfield => 1 + bitfieldSize,
            MessageType.Request or MessageType.Cancel => 13,
            MessageType.Block => 9 + (1 << 14),
            _ => -1,
        };
    }

    public static MessageHeader DecodeHeader(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out int length);
        reader.TryRead(out byte type);
        return new(length, (MessageType)type);
    }

    public static Handshake DecodeHandShake(ref SequenceReader<byte> reader)
    {
        string protocol = reader.ReadString(20);
        var extensions = BinaryPrimitives.ReadUInt64LittleEndian(reader.ReadBytes(8));
        var infoHash = reader.ReadBytes(20);
        var peerId = reader.ReadBytes(20);
        return new(protocol, extensions, infoHash, peerId);
    }

    public static BlockRequest DecodeRequest(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out int index);
        reader.TryReadBigEndian(out int begin);
        reader.TryReadBigEndian(out int length);
        return new(index, begin, length);
    }

    public static BlockShareHeader DecodePieceHeader(ref SequenceReader<byte> reader)
    {
        reader.TryReadBigEndian(out int index);
        reader.TryReadBigEndian(out int begin);
        return new(index, begin);
    }
}
