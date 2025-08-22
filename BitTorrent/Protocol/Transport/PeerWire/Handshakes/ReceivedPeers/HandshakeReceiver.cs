using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.Exceptions;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ReceivedPeers;

public class HandshakeReceiver : IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>>
{
    private readonly HandshakeHandler _handshakeHandler;

    public HandshakeReceiver(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }

    public DisposeHandle GetDisposer()
    {
        return new DisposeHandle(_handshakeHandler);
    }

    public async Task<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> ReadHandShakeAsync(CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.ReadHandShakeAsync(cancellationToken);
        return new HandshakeSender(_handshakeHandler);
    }
}
