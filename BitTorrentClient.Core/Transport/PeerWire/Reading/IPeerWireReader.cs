namespace BitTorrentClient.Core.Transport.PeerWire.Reading;

public interface IPeerWireReader : IDisposable, IAsyncDisposable
{
    Task<IMessageFrameReader> ReceiveAsync(CancellationToken cancellationToken = default);
}