using BitTorrent.Models.Messages;
using BitTorrent.Torrents.Peers;
using BitTorrent.Utils;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Encoding;
public static class MessageDecoder
{
    public const int HANDSHAKE_LEN = 49 + 19;
    public const int PIECE_HEADER_LEN = 8;
    public static HandShake DecodeHandShake(BigEndianBinaryReader reader)
    {
        var protocol = reader.ReadString();
        reader.Skip(8);
        var infoHash = reader.ReadBytes(20);
        var peerId = reader.ReadBytes(20);
        return new(protocol, infoHash.ToArray(), peerId.ToArray());
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
