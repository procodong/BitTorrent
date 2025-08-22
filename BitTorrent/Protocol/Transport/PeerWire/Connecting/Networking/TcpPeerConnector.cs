using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ContactedPeers;
using System.Net;
using System.Net.Sockets;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
public class TcpPeerConnector : IPeerConnector
{
    private readonly IPEndPoint _address;
    private readonly int _bufferSize;

    public TcpPeerConnector(IPEndPoint address, int bufferSize)
    {
        _address = address;
        _bufferSize = bufferSize;
    }

    public async Task<IHandshakeSender<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>>> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = new TcpClient();
        try
        {

            await client.ConnectAsync(_address, cancellationToken);
            var stream = new NetworkStream(client.Client, true);
            var buffer = new BufferCursor(_bufferSize);
            var handshake = new HandshakeHandler(stream, buffer);
            return new HandshakeSender(handshake);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public bool Equals(IPeerConnector? other)
    {
        return other is TcpPeerConnector o && o._address == _address;
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
