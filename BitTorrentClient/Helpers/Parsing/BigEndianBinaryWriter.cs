using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace BitTorrentClient.Helpers.Parsing;
public readonly struct BigEndianBinaryWriter
{
    private readonly IBufferWriter<byte> _writer;

    public BigEndianBinaryWriter(IBufferWriter<byte> writer)
    {
        _writer = writer;
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        var buffer = _writer.GetSpan(bytes.Length);
        bytes.CopyTo(buffer);
        _writer.Advance(bytes.Length);
    }

    public void Write(string text)
    {
        var buffer = _writer.GetSpan(text.Length);
        int len = Encoding.UTF8.GetBytes(text, buffer);
        _writer.Advance(len);
    }

    public void Write(byte value)
    {
        _writer.GetSpan(1)[0] = value;
        _writer.Advance(1);
    }

    public void Write(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(_writer.GetSpan(sizeof(short)), value);
        _writer.Advance(sizeof(short));
    }

    public void Write(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(_writer.GetSpan(sizeof(ushort)), value);
        _writer.Advance(sizeof(ushort));
    }

    public void Write(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(_writer.GetSpan(sizeof(int)), value);
        _writer.Advance(sizeof(int));
    }

    public void Write(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(_writer.GetSpan(sizeof(uint)), value);
        _writer.Advance(sizeof(uint));
    }

    public void Write(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(_writer.GetSpan(sizeof(long)), value);
        _writer.Advance(sizeof(long));
    }

    public void Write(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(_writer.GetSpan(sizeof(ulong)), value);
        _writer.Advance(sizeof(ulong));
    }
}
