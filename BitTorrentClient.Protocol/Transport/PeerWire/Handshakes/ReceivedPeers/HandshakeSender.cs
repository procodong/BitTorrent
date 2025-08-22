using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ReceivedPeers;

public class HandshakeSender : IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>
{
    private readonly HandshakeHandler _handshakeHandler;

    public HandshakeSender(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }

    public DisposeHandle GetDisposer()
    {
        return new DisposeHandle(_handshakeHandler);
    }

    public async Task<IBitfieldSender<PeerWireStream>> SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.SendHandShakeAsync(handshake, cancellationToken);
        return new BitfieldSender(_handshakeHandler);
    }

    public HandshakeData ReceiveHandshake => _handshakeHandler.ReceivedHandShake!.Value;
}