using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using BitTorrentClient.UserInterface.Input;

namespace BitTorrentClient.Application.Events.EventListening.Downloads;

public interface IDownloadEventHandler : ICommandContext
{
    Task OnTickAsync(CancellationToken cancellationToken = default);
    Task OnPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default);
}