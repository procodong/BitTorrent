using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ReceivedPeers;

public class HandshakeSender : IRespondedHandshakeSender<IBitfieldSender>
{
    private readonly HandshakeHandler _handshakeHandler;

    public HandshakeSender(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }
    
    public async Task<IBitfieldSender> SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.SendHandShakeAsync(handshake, cancellationToken);
        return new BitfieldSender(_handshakeHandler);
    }

    public HandshakeData ReceiveHandshake => _handshakeHandler.ReceivedHandShake!.Value;
}