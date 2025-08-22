using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.Interfaces;
public interface IPeerRelationHandler
{
    DataTransferVector GetRelation(PeerStatistics peerStatistics, DownloadStatistics downloadStatistics);
}
