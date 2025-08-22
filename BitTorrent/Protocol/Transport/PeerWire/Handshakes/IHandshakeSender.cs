using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public interface IHandshakeSender<TRet>
{
    Task<TRet> SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default);
}