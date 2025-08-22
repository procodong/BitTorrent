using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ContactedPeers;

public class HandshakeSender : IHandshakeSender<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>>
{
    private readonly HandshakeHandler _handler;

    public HandshakeSender(HandshakeHandler handler)
    {
        _handler = handler;
    }
    
    public async Task<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>> SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default)
    {
        await _handler.SendHandShakeAsync(handshake, cancellationToken);
        return new BitfieldSender(_handler);
    }
}