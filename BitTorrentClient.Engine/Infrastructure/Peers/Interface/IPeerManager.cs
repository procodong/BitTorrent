using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Engine.Infrastructure.Peers.Interface;
internal interface IPeerManager
{
    IPeerCollection Peers { get; }
    DownloadStatistics Statistics { get; }

    void ResetResentDataTransfer();
    IEnumerable<PeerStatistics> GetPeerStatistics();
    Task PauseAsync(PauseType type, CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task UpdateRelationsAsync(IEnumerable<DataTransferVector> relations, CancellationToken cancellationToken = default);
    Task NotifyPieceCompletion(int piece, CancellationToken cancellationToken = default);
    TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent);
}