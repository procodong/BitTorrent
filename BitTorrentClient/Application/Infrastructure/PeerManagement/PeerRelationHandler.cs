using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;

public class PeerRelationHandler : IPeerRelationHandler
{
    public DataTransferVector GetRelation(PeerStatistics peerStatistics, DownloadStatistics downloadStatistics)
    {
        DataTransferVector defaultPeerTransfer = downloadStatistics.MaxTransferRate / peerStatistics.DataTransfer;
        DataTransferVector averagePeerTransfer = downloadStatistics.TransferRate / downloadStatistics.PeerCount;
        return defaultPeerTransfer * (peerStatistics.DataTransfer / averagePeerTransfer);
    }
}