namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public interface IHandshakeReceiver<TRet>
{
    Task<TRet> ReadHandShakeAsync(CancellationToken cancellationToken = default);
}
