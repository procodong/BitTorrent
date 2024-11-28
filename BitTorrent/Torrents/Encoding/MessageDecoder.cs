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
        if (reader.BaseStream.CanSeek)
        {
            reader.BaseStream.Position += 8;
        }
        else
        {
            await reader.ReadBytesAsync(8);
        }
        var infoHash = await reader.ReadBytesAsync(20);
        var peerId = System.Text.Encoding.ASCII.GetString(await reader.ReadBytesAsync(20));
        return new(protocol, infoHash, peerId);
    }

    public static async Task<PieceRequest> DecodeRequestAsync(BigEndianBinaryReader reader)
    {
        var index = await reader.ReadInt32Async();
        var begin = await reader.ReadInt32Async();
        var length = await reader.ReadInt32Async();
        return new(index, begin, length);
    }

    public static async Task<PieceShareHeader> DecodePieceHeaderAsync(BigEndianBinaryReader reader)
    {
        var index = await reader.ReadInt32Async();
        var begin = await reader.ReadInt32Async();
        return new(index, begin);
    }
}
