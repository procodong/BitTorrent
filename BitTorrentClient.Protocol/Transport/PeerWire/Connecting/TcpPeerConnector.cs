using System.Net;
using System.Net.Sockets;
using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Interface;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
public sealed class TcpPeerConnector : IPeerConnector
{
    private readonly IPEndPoint _address;
    private readonly int _bufferSize;

    public TcpPeerConnector(IPEndPoint address, int bufferSize)
    {
        _address = address;
        _bufferSize = bufferSize;
    }

    public async Task<PendingPeerWireStream<InitialSendDataPhase>> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = new TcpClient();
        try
        {
            client.SendBufferSize = _bufferSize;
            await client.ConnectAsync(_address, cancellationToken);
            var stream = new NetworkStream(client.Client, true);
            var buffer = new BufferCursor(_bufferSize);
            var handshake = new HandshakeHandler(stream, buffer);
            return new PendingPeerWireStream<InitialSendDataPhase>(handshake);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public bool Equals(IPeerConnector? other)
    {
        return other is TcpPeerConnector o && o._address.Equals(_address);
    }

    public static bool operator ==(TcpPeerConnector first, TcpPeerConnector other) => first._address.Equals(other._address);
    public static bool operator !=(TcpPeerConnector first, TcpPeerConnector other) => !first._address.Equals(other._address);

    public override bool Equals(object? obj)
    {
        return obj is TcpPeerConnector t && t == this;
    }

    public override int GetHashCode()
    {
        return _address.GetHashCode();
    }
}
