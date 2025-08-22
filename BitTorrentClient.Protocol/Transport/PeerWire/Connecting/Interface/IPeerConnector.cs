using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
public interface IPeerConnector : IEquatable<IPeerConnector>
{
    Task<PendingPeerWireStream<InitialSendDataPhase>> ConnectAsync(CancellationToken cancellationToken = default);
}
