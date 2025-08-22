using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ContactedPeers;

public class HandshakeSender : IHandshakeSender<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>>
{
    private readonly HandshakeHandler _handshakeHandler;

    public HandshakeSender(HandshakeHandler handler)
    {
        _handshakeHandler = handler;
    }

    public DisposeHandle GetDisposer()
    {
        return new DisposeHandle(_handshakeHandler);
    }

    public async Task<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>> SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.SendHandShakeAsync(handshake, cancellationToken);
        return new BitfieldSender(_handshakeHandler);
    }
}