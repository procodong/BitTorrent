using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public interface IPeerManager
{
    IPeerCollection Peers { get; }
    IEnumerable<PeerStatistics> Statistics { get; }

    void ResetResentDataTransfer();
    Task PauseAsync(PauseType type, CancellationToken cancellationToken = default);
    Task ResumeAsync(CancellationToken cancellationToken = default);
    Task UpdateRelationsAsync(IEnumerable<PeerRelation> relations, CancellationToken cancellationToken = default);
    TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent);
}