using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public readonly struct BigEndianBinaryWriter
{
    private readonly MemoryStream _stream;
    public BigEndianBinaryWriter(MemoryStream output)
    {
        _stream = output;
    }

    private Span<byte> Remaining()
    {
        return _stream.GetBuffer().AsSpan((int)_stream.Position);
    }

    public void Write(Span<byte> bytes)
    {
        _stream.Write(bytes);
    }

    public void Write(string text)
    {
        var buffer = Remaining();
        foreach (var (i, character) in text.Indexed())
        {
            buffer[i] = (byte)character;
        }
        _stream.Position += buffer.Length;
    }

    public void Write(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(Remaining(), value);
        _stream.Position += sizeof(short);
    }

    public void Write(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(Remaining(), value);
        _stream.Position += sizeof(ushort);
    }

    public void Write(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(Remaining(), value);
        _stream.Position += sizeof(int);
    }

    public void Write(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(Remaining(), value);
        _stream.Position += sizeof(uint);
    }

    public void Write(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(Remaining(), value);
        _stream.Position += sizeof(long);
    }

    public void Write(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(Remaining(), value);
        _stream.Position += sizeof(ulong);
    }
}
