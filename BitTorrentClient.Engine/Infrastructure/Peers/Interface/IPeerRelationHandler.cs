using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Infrastructure.Peers.Interface;
internal interface IPeerRelationHandler
{
    DataTransferVector GetRelation(PeerStatistics peerStatistics, DownloadStatistics downloadStatistics);
}
