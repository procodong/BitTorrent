using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
public interface IPeerReceiver
{
    Task<PendingPeerWireStream<InitialReadDataPhase>> ReceivePeerAsync(CancellationToken cancellationToken = default);
}
