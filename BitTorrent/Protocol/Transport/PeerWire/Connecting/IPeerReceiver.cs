using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
public interface IPeerReceiver
{
    Task<IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>>> ReceivePeerAsync(CancellationToken cancellationToken = default);
}
