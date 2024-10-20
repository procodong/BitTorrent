using BitTorrent.Models.Messages;
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
    public static async Task<HandShake> DecodeHandShakeAsync(BigEndianBinaryReader reader)
    {
        var protocol = await reader.ReadStringAsync();
        reader.BaseStream.Position += 8;
        var infoHash = reader.ReadBytes(20);
        var peerId = Encoding.ASCII.GetString(reader.ReadBytes(20));
        return new(protocol, infoHash, peerId);
    }

    public static PieceRequest DecodeRequest(BigEndianBinaryReader reader)
    {
        var index = reader.ReadInt32();
        var begin = reader.ReadInt32();
        var length = reader.ReadInt32();
        return new(index, begin, length);
    }

    public static PieceShare DecodePieceHeader(BigEndianBinaryReader reader)
    {
        var index = reader.ReadInt32();
        var begin = reader.ReadInt32();
        return new(index, begin);
    }
}
