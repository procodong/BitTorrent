using System.IO.Pipelines;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Sending;
public sealed class PipedPeerWireWriter : IPeerWireWriter
{
    private readonly PipeWriter _pipe;
    private BigEndianBinaryWriter Writer => new(_pipe);

    public PipedPeerWireWriter(PipeWriter pipe)
    {
        _pipe = pipe;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _pipe.FlushAsync(cancellationToken);
    }

    public void SendCancel(BlockRequest cancel)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Cancel));
        MessageEncoder.EncodePieceRequest(Writer, cancel);
    }

    public void SendKeepAlive()
    {
        Writer.Write(0);
    }

    public void TryCancelUpload(BlockRequest cancel)
    {
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

    public void SendRequest(BlockRequest request)
    {
        MessageEncoder.EncodeHeader(Writer, new(13, MessageType.Request));
        MessageEncoder.EncodePieceRequest(Writer, request);
    }

    public async Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default)
    {
        MessageEncoder.EncodeHeader(Writer, new(block.Request.Length + 9, MessageType.Piece));
        MessageEncoder.EncodePieceHeader(Writer, new(block.Request.Index, block.Request.Begin));
        await block.Stream.CopyToAsync(_pipe, cancellationToken);
    }
}
