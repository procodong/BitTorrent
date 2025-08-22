using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public interface IRespondedHandshakeSender<TRet> : IHandshakeSender<TRet>
{
    HandshakeData ReceiveHandshake { get; }
}