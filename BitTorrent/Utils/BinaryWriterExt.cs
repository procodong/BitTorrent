using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public static class BinaryWriterExt
{
    public static async Task WriteAsync(this BinaryWriter writer, int value)
    {
        var buffer = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        await writer.BaseStream.WriteAsync(buffer);
    }
}
