using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
public interface IPeerConnector : IEquatable<IPeerConnector>
{
    Task<IHandshakeSender<IBitfieldSender<IHandshakeReceiver<PeerWireStream>>>> ConnectAsync(CancellationToken cancellationToken = default);
}
