using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IRespondedHandshakeSender<TRet> : IHandshakeSender<TRet>
{
    HandshakeData ReceiveHandshake { get; }
}