using System.IO.Pipelines;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Presentation.PeerWire;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Sending;
public class PipedMessageSender : IMessageSender
{
    private readonly PipeWriter _pipe;
    private BigEndianBinaryWriter Writer => new(_pipe);

    public PipedMessageSender(PipeWriter pipe)
    {
        _pipe = pipe;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _pipe.FlushAsync(cancellationToken);
    }

    public void SendCancel(PieceRequest cancel)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Cancel));
        MessageEncoder.EncodePieceRequest(Writer, cancel);
    }

    public void SendHave(int piece)
    {
        MessageEncoder.EncodeHeader(Writer, new(5, MessageType.Have));
        Writer.Write(piece);
    }

    public void SendRelation(RelationUpdate relation)
    {
        MessageEncoder.EncodeHeader(Writer, new(1, (MessageType)relation));
    }

    public void SendRequest(PieceRequest request)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(Writer, request);
    }

    public virtual async Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default)
    {
        MessageEncoder.EncodeHeader(Writer, new(block.Request.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(Writer, new(block.Request.Index, block.Request.Begin));
        await block.Stream.CopyToAsync(_pipe, cancellationToken);
    }
}
