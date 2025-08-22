using BitTorrentClient.Helpers.Parsing;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.ReceivedPeers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        var connection = await _listener.AcceptTcpClientAsync(cancellationToken);
        var stream = new NetworkStream(connection.Client, true);
        var buffer = new BufferCursor(_peerBufferSize);
        var handler = new HandshakeHandler(stream, buffer);
        return new HandshakeReceiver(handler);
    }
}
