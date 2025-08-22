using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Networking.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public interface IHandshakeFinisher
{
    HandshakeData ReceivedHandshake { get; }
    (IPeerWireReader, IMessageSender) Finish();
}