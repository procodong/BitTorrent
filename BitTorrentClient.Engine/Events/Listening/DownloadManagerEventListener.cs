using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;

namespace BitTorrentClient.Engine.Events.Listening;

public class DownloadManagerEventListener : IEventListener
{
    private readonly IDownloadManagerEventHandler _handler;
    private readonly IPeerReceiver _peerReceiver;

    public DownloadManagerEventListener(IDownloadManagerEventHandler handler, IPeerReceiver peerReceiver)
    {
        _handler = handler;
        _peerReceiver = peerReceiver;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var peer = await _peerReceiver.ReceivePeerAsync(cancellationToken);
            await _handler.OnPeerAsync(peer, cancellationToken);
        }
    }
}