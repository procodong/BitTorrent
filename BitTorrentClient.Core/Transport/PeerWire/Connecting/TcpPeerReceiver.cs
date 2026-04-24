using System.IO.Pipelines;
using System.Net.Sockets;
using BitTorrentClient.Core.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Core.Transport.PeerWire.Connecting;
public sealed class TcpPeerReceiver : IPeerReceiver
{
    private readonly TcpListener _listener;
    private readonly int _bufferSize;

    public TcpPeerReceiver(TcpListener listener, int peerBufferSize)
    {
        _listener = listener;
        _bufferSize = peerBufferSize;
    }

    public async Task<PendingPeerWireStream<InitialReadDataPhase>> ReceivePeerAsync(CancellationToken cancellationToken = default)
    {
        var client = await _listener.AcceptTcpClientAsync(cancellationToken);
        try
        {
            client.SendBufferSize = _bufferSize;
            var stream = new NetworkStream(client.Client, true);
            var reader = PipeReader.Create(stream, new(bufferSize: _bufferSize));
            var writer = PipeWriter.Create(stream, new(minimumBufferSize: _bufferSize));
            var handler = new HandshakeHandler(reader, writer);
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
