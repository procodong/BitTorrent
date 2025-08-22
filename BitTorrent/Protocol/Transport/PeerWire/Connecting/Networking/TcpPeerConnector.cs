using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes.ContactedPeers;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting.Networking;
public class TcpPeerConnector : IPeerConnector
{
    private readonly PeerAddress _address;
    private readonly int _bufferSize;

    public TcpPeerConnector(PeerAddress address, int bufferSize)
    {
        _address = address;
        _bufferSize = bufferSize;
    }

    public async Task<IHandshakeSender<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>>> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(_address.Ip, _address.Port, cancellationToken);
        var stream = new NetworkStream(client.Client, true);
        var buffer = new BufferCursor(_bufferSize);
        var handshake = new HandshakeHandler(stream, buffer);
        return new HandshakeSender(handshake);
    }
}
