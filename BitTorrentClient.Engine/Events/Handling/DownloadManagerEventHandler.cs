using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Infrastructure.Downloads.Interface;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Events.Handling;

public class DownloadManagerEventHandler : IDownloadManagerEventHandler
{
    private readonly IDownloadRepository _downloads;

    public DownloadManagerEventHandler(IDownloadRepository downloads)
    {
        _downloads = downloads;
    }

    public Task OnPeerAsync(PendingPeerWireStream<InitialReadDataPhase> peer, CancellationToken cancellationToken = default)
    {
        _ = _downloads.AddPeerAsync(peer, cancellationToken).Catch(_ => peer.DisposeAsync().AsTask());
        return Task.CompletedTask;
    }
}