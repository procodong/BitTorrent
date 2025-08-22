using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Application.Infrastructure.Peers;

namespace BitTorrentClient.Application.Launchers.Interface;
internal interface IPeerLauncher
{
    PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex);
}
