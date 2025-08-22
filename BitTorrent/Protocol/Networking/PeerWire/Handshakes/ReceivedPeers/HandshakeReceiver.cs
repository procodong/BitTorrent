using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.Exceptions;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ReceivedPeers;

public class HandshakeReceiver : IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender>>
{
    private readonly HandshakeHandler _handshakeHandler;

    public HandshakeReceiver(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }
    
    public async Task<IRespondedHandshakeSender<IBitfieldSender>> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        if (_handshakeHandler.ReceivedHandShake is not null) throw new AlreadyUsedException();
        await _handshakeHandler.ReadHandShakeAsync(cancellationToken);
        return new HandshakeSender(_handshakeHandler);
    }
}
