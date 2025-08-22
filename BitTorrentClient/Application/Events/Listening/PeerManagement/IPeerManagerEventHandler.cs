using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Listening.PeerManagement;
public interface IPeerManagerEventHandler
{
    Task OnPeerCreationAsync(PeerWireStream stream, CancellationToken cancellationToken = default);
    Task OnPeerRemovalAsync(int? peer, CancellationToken cancellationToken = default);
    Task OnPieceCompletionAsync(int piece, CancellationToken cancellationToken = default);
    Task OnTickAsync(CancellationToken cancellationToken = default);
    Task OnTrackerUpdate(TrackerResponse response, CancellationToken cancellationToken = default);
    Task OnStateChange(DownloadExecutionState change, CancellationToken cancellationToken = default);
    TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent);
}
