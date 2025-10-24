namespace BitTorrentClient.Core.Presentation.PeerWire.Models;
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
