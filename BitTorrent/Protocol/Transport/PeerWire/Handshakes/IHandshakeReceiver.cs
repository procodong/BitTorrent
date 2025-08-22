namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IHandshakeReceiver<TRet>
{
    Task<TRet> ReadHandShakeAsync(CancellationToken cancellationToken = default);
}
