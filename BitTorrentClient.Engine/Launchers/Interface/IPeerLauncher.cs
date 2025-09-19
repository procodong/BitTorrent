using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Launchers.Interface;
public interface IPeerLauncher
{
    PeerHandle LaunchPeer(PeerWireStream stream);
}
