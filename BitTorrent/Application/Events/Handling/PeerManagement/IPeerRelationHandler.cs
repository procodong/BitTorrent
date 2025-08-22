using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.EventHandling.PeerManagement;
public interface IPeerRelationHandler
{
    IEnumerable<PeerRelation> GetRelations(IEnumerable<PeerStatistics> peers);
}
