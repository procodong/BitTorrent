using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;

namespace BitTorrentClient.Application.Events.Listening.Downloads;

public class DownloadEventListener : IEventListener
{
    private readonly IDownloadEventHandler _hander;
    private readonly IPeerReceiver _peerReceiver;

    public DownloadEventListener(IDownloadEventHandler handler, IPeerReceiver peerReceiver)
    {
        _hander = handler;
        _peerReceiver = peerReceiver;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var peer = await _peerReceiver.ReceivePeerAsync(cancellationToken);
            await _hander.OnPeerAsync(peer, cancellationToken);
        }
    }
}