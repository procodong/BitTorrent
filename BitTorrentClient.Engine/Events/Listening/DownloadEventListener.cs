using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;

namespace BitTorrentClient.Engine.Events.Listening;

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