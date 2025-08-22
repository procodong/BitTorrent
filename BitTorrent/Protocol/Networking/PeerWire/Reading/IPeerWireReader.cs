namespace BitTorrentClient.Protocol.Networking.PeerWire;

public interface IPeerWireReader
{
    Task<IMessageFrameReader> ReceiveAsync(CancellationToken cancellationToken = default);
}