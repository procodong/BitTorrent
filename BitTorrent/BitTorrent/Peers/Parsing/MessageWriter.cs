using BitTorrentClient.Models.Messages;
using BitTorrentClient.Utils.Parsing;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers.Parsing;
public readonly struct MessageWriter
{
    private readonly IBufferWriter<byte> _buffer;
    private BigEndianBinaryWriter Writer => new(_buffer);

    public MessageWriter(IBufferWriter<byte> buffer)
    {
        _buffer = buffer;
    }

    public void WriteUpdateRelation(Relation relation)
    {
        MessageEncoder.EncodeHeader(Writer, new(1, (MessageType)relation));
    }

    public void WriteHaveMessage(int piece)
    {
        MessageEncoder.EncodeHeader(Writer, new(5, MessageType.Have));
        Writer.Write(piece);
    }

    public void WritePieceRequest(PieceRequest request)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(Writer, request);
    }
}