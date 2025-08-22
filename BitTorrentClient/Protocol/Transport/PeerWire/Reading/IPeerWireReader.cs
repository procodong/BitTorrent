namespace BitTorrentClient.Protocol.Transport.PeerWire.Reading;

public interface IPeerWireReader
{
    Task<IMessageFrameReader> ReceiveAsync(CancellationToken cancellationToken = default);
}