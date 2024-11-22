using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public static class BinaryReaderExt
{
    public static async Task<string> ReadStringAsync(this BinaryReader reader)
    {
        var buffer = new byte[1];
        await reader.BaseStream.ReadExactlyAsync(buffer);
        int len = buffer[0];
        byte[] stringBytes = new byte[len];
        return Encoding.ASCII.GetString(stringBytes);
    }

    public static async Task<byte[]> ReadBytesAsync(this BinaryReader reader, int count)
    {
        var buffer = new byte[count];
        await reader.BaseStream.ReadExactlyAsync(buffer);
        return buffer;
    }
}
