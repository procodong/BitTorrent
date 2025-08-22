using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.EventListening.PeerManagement;
public interface IPeerManagerEventHandler
{
    Task OnPeerCreationAsync(PeerWireStream stream, CancellationToken cancellationToken = default);
    Task OnPeerRemovalAsync(int? peer, CancellationToken cancellationToken = default);
    Task OnTickAsync(CancellationToken cancellationToken = default);
    Task OnTrackerUpdate(TrackerResponse response, CancellationToken cancellationToken = default);
    Task OnStateChange(DownloadExecutionState change, CancellationToken cancellationToken = default);
    TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent);
}
