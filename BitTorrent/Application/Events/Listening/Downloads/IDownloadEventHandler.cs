using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Listening.Downloads;

public interface IDownloadEventHandler : ICommandContext
{
    Task OnTickAsync(CancellationToken cancellationToken = default);
    Task OnPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default);
}