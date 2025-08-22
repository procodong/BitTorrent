using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils.Parsing;
public readonly struct BigEndianBinaryReader
{
    private readonly BufferCursor _cursor;

    public BufferCursor Cursor => _cursor;
    public ref int Position => ref _cursor.Position;
    public ref int Length => ref _cursor.Length;

    public BigEndianBinaryReader(BufferCursor cursor)
    {
        _cursor = cursor;
    }

    private ReadOnlySpan<byte> Remaining => _cursor.Buffer.AsSpan(Position..Length);

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

    public string ReadString(int len)
    {
        var text = Encoding.UTF8.GetString(ReadBytes(len));
        return text;
    }

    public string ReadString()
    {
        int len = ReadByte();
        return ReadString(len);
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
