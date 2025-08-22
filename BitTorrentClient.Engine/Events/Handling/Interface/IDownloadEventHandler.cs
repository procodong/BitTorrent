using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Events.Handling.Interface;

public interface IDownloadEventHandler
{
    Task OnPeerAsync(PendingPeerWireStream<InitialReadDataPhase> peer, CancellationToken cancellationToken = default);
}