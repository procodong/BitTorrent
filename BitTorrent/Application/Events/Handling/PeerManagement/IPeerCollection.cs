using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.EventHandling.PeerManagement;
public interface IPeerCollection
{
    void Add(PeerWireStream peer);
    void Remove(int? peer);
    void Feed(IEnumerable<IPeerConnector> peers);
}
