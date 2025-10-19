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
        var client = await _listener.AcceptTcpClientAsync(cancellationToken);
        try
        {
            client.SendBufferSize = _peerBufferSize;
            var stream = new NetworkStream(client.Client, true);
            var buffer = new BufferCursor(_peerBufferSize);
            var handler = new HandshakeHandler(stream, buffer);
            return new(handler);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
