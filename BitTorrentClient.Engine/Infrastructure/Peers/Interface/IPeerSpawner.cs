using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Peers.Interface;
public interface IPeerSpawner
{
    Task SpawnConnect(PendingPeerWireStream<SendDataPhase> peer, CancellationToken cancellationToken = default);
}
