using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.Peers.Interface;
internal interface IPeerSpawner
{
    Task SpawnConnect(IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>> peer, CancellationToken cancellationToken = default);
}
