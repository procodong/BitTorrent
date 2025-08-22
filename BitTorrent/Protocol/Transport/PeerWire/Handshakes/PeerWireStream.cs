using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Networking.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public class PeerWireStream
{
    public HandshakeData ReceivedHandshake { get; }
    public IPeerWireReader Reader { get; }
    public IMessageSender Sender { get; }

    public PeerWireStream(HandshakeData receivedHandshake, IPeerWireReader reader, IMessageSender sender)
    {
        ReceivedHandshake = receivedHandshake;
        Reader = reader;
        Sender = sender;
    }
}