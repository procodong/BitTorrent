using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Launchers.Interface;
internal interface IPeerLauncher
{
    PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex);
}
