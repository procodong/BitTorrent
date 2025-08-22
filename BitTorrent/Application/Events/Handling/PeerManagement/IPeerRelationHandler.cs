using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public interface IPeerRelationHandler
{
    IEnumerable<PeerRelation> GetRelations(IEnumerable<PeerStatistics> peers);
}
