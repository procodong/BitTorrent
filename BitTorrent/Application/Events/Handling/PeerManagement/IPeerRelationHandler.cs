using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public interface IPeerRelationHandler
{
    IEnumerable<PeerStatistics> GetRelations(IEnumerable<PeerStatistics> peers);
}
