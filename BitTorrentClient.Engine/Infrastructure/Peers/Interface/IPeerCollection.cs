using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Peers.Interface;
internal interface IPeerCollection
{
    void Add(PeerWireStream peer);
    void Remove(int? peer);
    void Feed(IEnumerable<IPeerConnector> peers);
}
