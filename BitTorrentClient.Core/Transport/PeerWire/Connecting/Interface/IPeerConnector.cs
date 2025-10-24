using BitTorrentClient.Core.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Core.Transport.PeerWire.Connecting.Interface;
public interface IPeerConnector : IEquatable<IPeerConnector>
{
    Task<PendingPeerWireStream<InitialSendDataPhase>> ConnectAsync(CancellationToken cancellationToken = default);
}
