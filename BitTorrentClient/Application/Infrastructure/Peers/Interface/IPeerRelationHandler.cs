using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.Peers.Interface;
internal interface IPeerRelationHandler
{
    DataTransferVector GetRelation(PeerStatistics peerStatistics, DownloadStatistics downloadStatistics);
}
