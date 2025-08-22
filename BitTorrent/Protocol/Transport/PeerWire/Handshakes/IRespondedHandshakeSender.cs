using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public interface IRespondedHandshakeSender<TRet> : IHandshakeSender<TRet>
{
    HandshakeData ReceiveHandshake { get; }
}