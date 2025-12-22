using System.Buffers;
using System.Text;

namespace BitTorrentClient.Helpers.Extensions;

public static class SequenceReaderExt
{

    public static string ReadString(this ref SequenceReader<byte> reader, int length)
    {
        return Encoding.UTF8.GetString(reader.ReadBytes(length));
    }
    
    public static byte[] ReadBytes(this ref SequenceReader<byte> reader, int length)
    {
        var stringBytes = reader.Sequence.Slice(reader.Position, length);
        reader.Advance(length);
        return stringBytes.ToArray();
    }
}