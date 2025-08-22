using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
public interface IPeerConnector : IEquatable<IPeerConnector>
{
    Task<IHandshakeSender<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>>> ConnectAsync(CancellationToken cancellationToken = default);
}
