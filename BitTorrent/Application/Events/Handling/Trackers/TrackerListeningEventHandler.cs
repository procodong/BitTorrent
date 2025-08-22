using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Trackers;
using System.Net.Sockets;
using BitTorrentClient.Application.Events.Listening.Trackers;

namespace BitTorrentClient.Application.Events.Handling.Trackers;
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
        var handshaker = new HandshakeHandler(stream, buffer);
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
