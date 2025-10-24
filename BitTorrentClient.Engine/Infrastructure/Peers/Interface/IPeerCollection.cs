using BitTorrentClient.Core.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Peers.Interface;
public interface IPeerCollection
{
    void Add(PeerWireStream peer);
    void Remove(ReadOnlyMemory<byte>? id);
    void Feed(IEnumerable<IPeerConnector> peers);
}
