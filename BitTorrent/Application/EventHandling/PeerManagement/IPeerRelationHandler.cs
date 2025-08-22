using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.BitTorrent.Peers;
using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.PeerManagement;
public interface IPeerRelationHandler
{
    IEnumerable<PeerRelation> GetRelations(IEnumerable<PeerStatistics> peers);
}
