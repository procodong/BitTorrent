using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public interface IPeerCollection
{
    void Add(PeerWireStream peer);
    void Remove(int? peer);
    void Feed(IEnumerable<IPeerConnector> peers);
}
