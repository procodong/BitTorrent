namespace BitTorrentClient.Models.Messages;
public enum MessageType
{
    Choke,
    UnChoke,
    Interested,
    NotInterested,
    Have,
    Bitfield,
    Request,
    Piece,
    Cancel,
}
