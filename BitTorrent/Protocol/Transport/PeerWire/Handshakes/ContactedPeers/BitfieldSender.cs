using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ContactedPeers;

public class BitfieldSender : IBitfieldSender<IHandshakeReceiver<PeerWireStream>>
{
    private readonly HandshakeHandler _handshakeHandler;

    public BitfieldSender(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }
    
    public async Task<IHandshakeReceiver<PeerWireStream>> SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.SendBitfieldAsync(bitfield, cancellationToken);
        return new HandshakeReceiver(_handshakeHandler);
    }
}