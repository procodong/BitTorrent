using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public class BigEndianBinaryReader : BinaryReader
{
    public BigEndianBinaryReader(Stream input) : base(input)
    {
    }

    public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
    {
    }

    public override short ReadInt16()
    {
        Span<byte> buffer = stackalloc byte[2];
        BaseStream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    public override int ReadInt32()
    {
        Span<byte> buffer = stackalloc byte[4];
        BaseStream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }
    public override long ReadInt64()
    {
        Span<byte> buffer = stackalloc byte[8];
        BaseStream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    public override ushort ReadUInt16()
    {
        Span<byte> buffer = stackalloc byte[2];
        BaseStream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    public override uint ReadUInt32()
    {
        Span<byte> buffer = stackalloc byte[4];
        BaseStream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }
    public override ulong ReadUInt64()
    {
        Span<byte> buffer = stackalloc byte[8];
        BaseStream.ReadExactly(buffer);
        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }
}
