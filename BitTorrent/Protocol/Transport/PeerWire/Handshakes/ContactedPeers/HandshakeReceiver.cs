using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ContactedPeers;

public class HandshakeReceiver : IHandshakeReceiver<PeerWireStream>
{
    private readonly HandshakeHandler _handshakeHandler;

    public HandshakeReceiver(HandshakeHandler handler)
    {
        _handshakeHandler = handler;
    }

    public IAsyncDisposable GetDisposer()
    {
        return new DisposeHandle(_handshakeHandler);
    }

    public async Task<PeerWireStream> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.ReadHandShakeAsync(cancellationToken);
        return _handshakeHandler.Finish();
    }
}