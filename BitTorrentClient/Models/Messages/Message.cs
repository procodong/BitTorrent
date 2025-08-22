namespace BitTorrentClient.Models.Messages;
public readonly struct Message
{
    private readonly MessageType _type;
    private readonly MessageUnion _message;
    private readonly Stream _body;

    public MessageType Type => _type;
    public MessageUnion Data => _message;
    public Stream Body => _body;

    public Message(RelationUpdate update)
    {
        _type = (MessageType)update;
        _body = null!;
    }

    public Message(BlockRequest request, RequestType type)
    {
        _type = (MessageType)type;
        _message = new MessageUnion(request);
        _body = null!;
    }

    public Message(int have)
    {
        _type = MessageType.Have;
        _message = new(have);
        _body = null!;
    }

    public Message(BlockData block)
    {
        _body = block.Stream;
        _message = new((BlockShareHeader)block.Request);
    }
}

public enum RequestType
{
    Request = 6,
    Cancel = 8,
}