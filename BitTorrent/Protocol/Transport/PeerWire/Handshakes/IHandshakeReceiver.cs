using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IHandshakeReceiver<TRet> : IDisposeHandleProvider
{
    Task<TRet> ReadHandShakeAsync(CancellationToken cancellationToken = default);
}
