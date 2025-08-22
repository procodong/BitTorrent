using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IHandshakeSender<TRet> : IDisposeHandleProvider
{
    Task<TRet> SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default);
}