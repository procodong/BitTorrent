using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Listening.Downloads;

public interface IDownloadEventHandler
{
    Task OnPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default);
}