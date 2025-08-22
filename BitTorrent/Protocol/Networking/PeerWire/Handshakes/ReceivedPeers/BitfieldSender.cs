using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ReceivedPeers;

public class BitfieldSender : IBitfieldSender
{
    private readonly HandshakeHandler _handshakeHandler;

    public BitfieldSender(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }
    
    public async Task<IHandshakeFinisher> SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.SendBitfieldAsync(bitfield, cancellationToken);
        return new HandshakeFinisher(_handshakeHandler);
    }
}