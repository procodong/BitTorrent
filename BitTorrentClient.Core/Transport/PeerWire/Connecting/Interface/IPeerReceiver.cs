using BitTorrentClient.Core.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Core.Transport.PeerWire.Connecting.Interface;
public interface IPeerReceiver : IDisposable
{
    Task<PendingPeerWireStream<InitialReadDataPhase>> ReceivePeerAsync(CancellationToken cancellationToken = default);
}
