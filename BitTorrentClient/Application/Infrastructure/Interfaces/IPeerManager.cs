using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
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