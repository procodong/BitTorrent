using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Application.Infrastructure.PeerManagement;

namespace BitTorrentClient.Application.Launchers.Peers;
public interface IPeerLauncher
{
    PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex);
}
