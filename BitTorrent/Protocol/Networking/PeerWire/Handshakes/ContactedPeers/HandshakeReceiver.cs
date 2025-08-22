namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ContactedPeers;

public class HandshakeReceiver : IHandshakeReceiver<IHandshakeSender<IBitfieldSender>>
{
    private readonly HandshakeHandler _handler;

    public HandshakeReceiver(HandshakeHandler handler)
    {
        _handler = handler;
    }
    
    public async Task<IHandshakeSender<IBitfieldSender>> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _handler.ReadHandShakeAsync(cancellationToken);
        return new HandshakeSender(_handler);
    }
}