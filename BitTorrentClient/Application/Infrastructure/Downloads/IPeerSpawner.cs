using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public interface IPeerSpawner
{
    Task SpawnConnect(IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>> peer, CancellationToken cancellationToken = default);
}
