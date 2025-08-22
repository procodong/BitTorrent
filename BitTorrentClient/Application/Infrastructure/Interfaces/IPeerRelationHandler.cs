using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public interface IPeerRelationHandler
{
    DataTransferVector GetRelation(PeerStatistics peerStatistics, DownloadStatistics downloadStatistics);
}
