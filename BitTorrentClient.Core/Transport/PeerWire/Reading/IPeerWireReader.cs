using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Core.Transport.PeerWire.Reading;

public interface IPeerWireReader : IDisposable, IAsyncDisposable
{
    Task<(MessageType, MessageData)> ReceiveAsync(CancellationToken cancellationToken = default);
}