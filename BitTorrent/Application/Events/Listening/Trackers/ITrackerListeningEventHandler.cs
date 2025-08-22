using BitTorrentClient.Models.Trackers;
using System.Net.Sockets;

namespace BitTorrentClient.Application.Events.Listening.Trackers;
public interface ITrackerListeningEventHandler
{
    Task OnNewPeerAsync(TcpClient client, CancellationToken cancellationToken = default);
    Task OnPeerReceivingSubscription(PeerReceivingSubscribe subscription, CancellationToken cancellationToken = default);
}
