using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public class BigEndianBinaryWriter
{
    private readonly byte[] _buffer;

    public int Position { get; set; }

    public BigEndianBinaryWriter(byte[] buffer)
    {
        _buffer = buffer;
    }

    public BigEndianBinaryWriter(byte[] buffer, int position) : this(buffer)
    {
        Position = position;
    }

    private Span<byte> Remaining => _buffer.AsSpan(Position);

    public void Write(ReadOnlySpan<byte> bytes)
    {
        bytes.CopyTo(Remaining);
        Position += bytes.Length;
    }

    public void Write(string text)
    {
        var buffer = Remaining;
        for (int i = 0; i < text.Length; i++)
        {
            buffer[i] = (byte)text[i];
        }
        Position += text.Length;
    }

    public void Write(byte value)
    {
        Remaining[0] = value;
        Position++;
    }

    public void Write(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(Remaining, value);
        Position += sizeof(short);
    }

    public void Write(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(Remaining, value);
        Position += sizeof(ushort);
    }

    public void Write(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(Remaining, value);
        Position += sizeof(int);
    }

    public void Write(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(Remaining, value);
        Position += sizeof(uint);
    }

    public void Write(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(Remaining, value);
        Position += sizeof(long);
    }

    public void Write(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(Remaining, value);
        Position += sizeof(ulong);
    }
}
