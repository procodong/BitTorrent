using System.Net.Sockets;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
public sealed class TcpPeerReceiver : IPeerReceiver
{
    private readonly TcpListener _listener;
    private readonly int _peerBufferSize;

    public TcpPeerReceiver(TcpListener listener, int peerBufferSize)
    {
        _listener = listener;
        _peerBufferSize = peerBufferSize;
    }

    public async Task<PendingPeerWireStream<InitialReadDataPhase>> ReceivePeerAsync(CancellationToken cancellationToken = default)
    {
        var socket = await _listener.AcceptSocketAsync(cancellationToken);
        var stream = new NetworkStream(socket, true);
        var buffer = new BufferCursor(_peerBufferSize);
        var handler = new HandshakeHandler(stream, buffer);
        return new(handler);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
