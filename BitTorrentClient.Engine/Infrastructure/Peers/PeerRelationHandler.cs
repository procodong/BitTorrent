using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Infrastructure.Peers;

public class PeerRelationHandler : IPeerRelationHandler
{
    public DataTransferVector GetRelation(PeerStatistics peerStatistics, DownloadStatistics downloadStatistics)
    {
        var defaultPeerTransfer = downloadStatistics.MaxTransferRate / peerStatistics.DataTransfer;
        var averagePeerTransfer = downloadStatistics.TransferRate / downloadStatistics.PeerCount;
        return defaultPeerTransfer * (peerStatistics.DataTransfer / averagePeerTransfer);
    }
}