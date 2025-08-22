namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ContactedPeers;

public class HandshakeReceiver : IHandshakeReceiver<PeerWireStream>
{
    private readonly HandshakeHandler _handler;

    public HandshakeReceiver(HandshakeHandler handler)
    {
        _handler = handler;
    }

    public async Task<PeerWireStream> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _handler.ReadHandShakeAsync(cancellationToken);
        return _handler.Finish();
    }
}