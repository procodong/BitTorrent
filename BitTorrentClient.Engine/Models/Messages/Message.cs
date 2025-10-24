using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Models.Messages;
public readonly struct Message
{
    public MessageType Type { get; }
    public MessageUnion Data { get; }
    public Stream Body { get; }

    public Message(RelationUpdate update)
    {
        Type = (MessageType)update;
        Body = null!;
    }

    public Message(BlockRequest request, RequestType type)
    {
        Type = (MessageType)type;
        Data = new MessageUnion(request);
        Body = null!;
    }

    public Message(int have)
    {
        Type = MessageType.Have;
        Data = new(have);
        Body = null!;
    }

    public Message(BlockData block)
    {
        Body = block.Stream;
        Data = new((BlockShareHeader)block.Request);
    }
}

public enum RequestType
{
    Request = 6,
    Cancel = 8,
}