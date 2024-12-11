using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public readonly struct BigEndianBinaryReader
{
    private readonly MemoryStream _stream;

    public MemoryStream BaseStream => _stream;
    public BigEndianBinaryReader(MemoryStream stream)
    {
        _stream = stream;
    }

    private Span<byte> Remaining()
    {
        return _stream.GetBuffer().AsSpan((int)_stream.Position);
    }

    public void Skip(int count)
    {
        _stream.Position += count;
    }

    public byte ReadByte()
    {
        var b = Remaining()[0];
        _stream.Position++;
        return b;
    }

    public Span<byte> ReadBytes(int count)
    {
        var bytes = Remaining()[..count];
        _stream.Position += count;
        return bytes;
    }

    public string ReadString()
    {
        int len = ReadByte();
        var text = Encoding.UTF8.GetString(ReadBytes(len));
        _stream.Position += len;
        return text;
    }

    public short ReadInt16()
    {
        var num = BinaryPrimitives.ReadInt16BigEndian(Remaining());
        _stream.Position += sizeof(short);
        return num;
    }
    public ushort ReadUInt16()
    {
        var num = BinaryPrimitives.ReadUInt16BigEndian(Remaining());
        _stream.Position += sizeof(ushort);
        return num;
    }

    public int ReadInt32()
    {
        var num = BinaryPrimitives.ReadInt32BigEndian(Remaining());
        _stream.Position += sizeof(int);
        return num;
    }
    public uint ReadUInt32()
    {
        var num = BinaryPrimitives.ReadUInt32BigEndian(Remaining());
        _stream.Position += sizeof(uint);
        return num;
    }

    public long ReadInt64()
    {
        var num = BinaryPrimitives.ReadInt64BigEndian(Remaining());
        _stream.Position += sizeof(long);
        return num;
    }
    public ulong ReadUInt64()
    {
        var num = BinaryPrimitives.ReadUInt64BigEndian(Remaining());
        _stream.Position += sizeof(ulong);
        return num;
    }
}
