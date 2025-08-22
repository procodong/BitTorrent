using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.PeerManagement;

namespace BitTorrentClient.Application.Launchers.Peers;
public interface IPeerLauncher
{
    PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex);
}
