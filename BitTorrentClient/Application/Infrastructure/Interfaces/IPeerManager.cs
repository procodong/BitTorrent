using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;

namespace BitTorrentClient.Application.Infrastructure.Interfaces;
public interface IPeerManager
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