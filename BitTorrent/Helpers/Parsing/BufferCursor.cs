using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils.Parsing;
public class BufferCursor
{
    public byte[] Buffer;
    public int Position;
    public int Length;

    public BufferCursor(byte[] buffer) : this(buffer, 0, buffer.Length)
    {
    }
    public BufferCursor(byte[] buffer, int position, int length)
    {
        Buffer = buffer;
        Position = position;
        Length = length;
    }
    public int RemainingBytes => Length - Position;
}
