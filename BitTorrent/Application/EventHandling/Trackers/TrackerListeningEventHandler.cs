using BitTorrentClient.Application.EventListening.Trackers;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.Trackers;
public class TrackerListeningEventHandler : ITrackerListeningEventHandler
{
    private readonly ITrackerListeningHandler _handler;
    private readonly int _peerBufferSize;

    public TrackerListeningEventHandler(ITrackerListeningHandler handler, int peerBufferSize)
    {
        _handler = handler;
        _peerBufferSize = peerBufferSize;
    }

    public async Task OnNewPeerAsync(TcpClient client, CancellationToken cancellationToken = default)
    {
        var stream = new NetworkStream(client.Client, true);
        var buffer = new BufferCursor(_peerBufferSize);
        var handshaker = new PeerHandshaker(stream, buffer);
        var handshake = await handshaker.ReadHandShakeAsync(cancellationToken);
        await _handler.SendPeerAsync(handshaker, cancellationToken);
    }

    public Task OnPeerReceivingSubscription(PeerReceivingSubscribe subscription, CancellationToken cancellationToken = default)
    {
        if (subscription.EventWriter is not null)
        {
            _handler.AddDownload(subscription.InfoHash, subscription.EventWriter);
        } 
        else
        {
            _handler.RemoveDownload(subscription.InfoHash);
        }
        return Task.CompletedTask;
    }
}
