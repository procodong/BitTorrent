using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.PeerManagement;
public interface IPeerCollection
{
    void Add(RespondedPeerHandshaker peer);
    void Remove(int? peer);
    void Feed(PeerAddress[] peers);
}
