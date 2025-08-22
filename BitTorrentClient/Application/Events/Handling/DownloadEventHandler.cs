using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling;

public class DownloadEventHandler : IDownloadEventHandler
{
    private readonly IDownloadCollection _downloads;

    public DownloadEventHandler(IDownloadCollection downloads)
    {
        _downloads = downloads;
    }

    public Task OnPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer, CancellationToken cancellationToken = default)
    {
        _ = _downloads.AddPeerAsync(peer, cancellationToken);
        return Task.CompletedTask;
    }
}