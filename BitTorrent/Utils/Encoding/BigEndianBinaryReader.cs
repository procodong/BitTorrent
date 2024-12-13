using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public class BigEndianBinaryReader
{
    private readonly byte[] _buffer;

    public int Position { get; set; }
    public int Length { get; set; }
    public BigEndianBinaryReader(byte[] buffer)
    {
        _buffer = buffer;
        Length = buffer.Length;
    }

    public BigEndianBinaryReader(byte[] buffer, int position) : this(buffer)
    {
        Position = position;
    }

    public BigEndianBinaryReader(byte[] buffer, int position, int length) : this(buffer, position)
    {
        Length = length;
    }

    private ReadOnlySpan<byte> Remaining => _buffer.AsSpan(Position..Length);

    public void Skip(int count)
    {
        Position += count;
    }

    public byte ReadByte()
    {
        var b = Remaining[0];
        Position++;
        return b;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var bytes = Remaining[..count];
        Position += count;
        return bytes;
    }

    public string ReadString()
    {
        int len = ReadByte();
        var text = Encoding.UTF8.GetString(ReadBytes(len));
        return text;
    }

    public short ReadInt16()
    {
        var num = BinaryPrimitives.ReadInt16BigEndian(Remaining);
        Position += sizeof(short);
        return num;
    }
    public ushort ReadUInt16()
    {
        var num = BinaryPrimitives.ReadUInt16BigEndian(Remaining);
        Position += sizeof(ushort);
        return num;
    }

    public int ReadInt32()
    {
        var num = BinaryPrimitives.ReadInt32BigEndian(Remaining);
        Position += sizeof(int);
        return num;
    }
    public uint ReadUInt32()
    {
        var num = BinaryPrimitives.ReadUInt32BigEndian(Remaining);
        Position += sizeof(uint);
        return num;
    }

    public long ReadInt64()
    {
        var num = BinaryPrimitives.ReadInt64BigEndian(Remaining);
        Position += sizeof(long);
        return num;
    }
    public ulong ReadUInt64()
    {
        var num = BinaryPrimitives.ReadUInt64BigEndian(Remaining);
        Position += sizeof(ulong);
        return num;
    }
}
