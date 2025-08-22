using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Infrastructure.Peers.Interface;
internal interface IPeerCollection
{
    void Add(PeerWireStream peer);
    void Remove(int? peer);
    void Feed(IEnumerable<IPeerConnector> peers);
}
