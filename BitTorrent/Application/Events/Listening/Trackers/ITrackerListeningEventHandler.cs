using BitTorrentClient.Models.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.EventListening.Trackers;
public interface ITrackerListeningEventHandler
{
    Task OnNewPeerAsync(TcpClient client, CancellationToken cancellationToken = default);
    Task OnPeerReceivingSubscription(PeerReceivingSubscribe subscription, CancellationToken cancellationToken = default);
}
