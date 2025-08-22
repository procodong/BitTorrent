using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ReceivedPeers;

public class BitfieldSender : IBitfieldSender<PeerWireStream>
{
    private readonly HandshakeHandler _handshakeHandler;

    public BitfieldSender(HandshakeHandler handshakeHandler)
    {
        _handshakeHandler = handshakeHandler;
    }

    public async Task<PeerWireStream> SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        await _handshakeHandler.SendBitfieldAsync(bitfield, cancellationToken);
        return _handshakeHandler.Finish();
    }
}