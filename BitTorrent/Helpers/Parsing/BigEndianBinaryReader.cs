using System.Buffers.Binary;
using System.Text;

namespace BitTorrentClient.Helpers.Parsing;
public readonly struct BigEndianBinaryReader
{
    private readonly IBufferReader _reader;

    public BigEndianBinaryReader(IBufferReader reader)
    {
        _reader = reader;
    }

    public int Remaining => _reader.GetSpan().Length;

    public byte ReadByte()
    {
        var b = _reader.GetSpan()[0];
        _reader.Advance(1);
        return b;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var bytes = _reader.GetSpan()[..count];
        _reader.Advance(count);
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
        var num = BinaryPrimitives.ReadInt16BigEndian(_reader.GetSpan());
        _reader.Advance(sizeof(short));
        return num;
    }
    public ushort ReadUInt16()
    {
        var num = BinaryPrimitives.ReadUInt16BigEndian(_reader.GetSpan());
        _reader.Advance(sizeof(ushort));
        return num;
    }

    public int ReadInt32()
    {
        var num = BinaryPrimitives.ReadInt32BigEndian(_reader.GetSpan());
        _reader.Advance(sizeof(int));
        return num;
    }
    public uint ReadUInt32()
    {
        var num = BinaryPrimitives.ReadUInt32BigEndian(_reader.GetSpan());
        _reader.Advance(sizeof(uint));
        return num;
    }

    public long ReadInt64()
    {
        var num = BinaryPrimitives.ReadInt64BigEndian(_reader.GetSpan());
        _reader.Advance(sizeof(long));
        return num;
    }
    public ulong ReadUInt64()
    {
        var num = BinaryPrimitives.ReadUInt64BigEndian(_reader.GetSpan());
        _reader.Advance(sizeof(ulong));
        return num;
    }
}
