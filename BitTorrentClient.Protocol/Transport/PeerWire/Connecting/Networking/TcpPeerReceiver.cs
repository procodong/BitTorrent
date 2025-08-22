using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ReceivedPeers;
using System.Net.Sockets;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
public class TcpPeerReceiver : IPeerReceiver
{
    private readonly TcpListener _listener;
    private readonly int _peerBufferSize;

    public TcpPeerReceiver(TcpListener listener, int peerBufferSize)
    {
        _listener = listener;
        _peerBufferSize = peerBufferSize;
    }

    public async Task<IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>>> ReceivePeerAsync(CancellationToken cancellationToken = default)
    {
        var socket = await _listener.AcceptSocketAsync(cancellationToken);
        var stream = new NetworkStream(socket, true);
        var buffer = new BufferCursor(_peerBufferSize);
        var handler = new HandshakeHandler(stream, buffer);
        return new HandshakeReceiver(handler);
    }
}
