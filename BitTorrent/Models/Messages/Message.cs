using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Models.Messages;
public struct Message
{
    private readonly MessageType _type;
    private readonly MessageUnion _message;

    public MessageType Type => _type;
    public MessageUnion Data => _message;

    public Message(RelationUpdate update)
    {
        _type = (MessageType)update;
    }

    public Message(PieceRequest request, RequestType type)
    {
        _type = (MessageType)type;
        _message = new MessageUnion(request);
    }

    public Message(int have)
    {
        _type = MessageType.Have;
        _message = new(have);
    }
}

public enum RequestType
{
    Request = 6,
    Cancel = 8,
}