using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Core.Presentation.UdpTracker.Models;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Events.Handling.Interface;
public interface IPeerManagerEventHandler
{
    Task OnPeerCreationAsync(PeerWireStream stream, CancellationToken cancellationToken = default);
    Task OnPeerRemovalAsync(ReadOnlyMemory<byte>? peer, CancellationToken cancellationToken = default);
    Task OnPieceCompletionAsync(int piece, CancellationToken cancellationToken = default);
    Task OnTickAsync(CancellationToken cancellationToken = default);
    Task OnTrackerUpdate(TrackerResponse response, CancellationToken cancellationToken = default);
    Task OnStateChange(DownloadExecutionState change, CancellationToken cancellationToken = default);
    TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent);
}
