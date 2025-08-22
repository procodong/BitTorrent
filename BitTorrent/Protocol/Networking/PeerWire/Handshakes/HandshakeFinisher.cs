using BitTorrentClient.Application.Infrastructure.MessageWriting;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Networking.PeerWire.Reading;
using BitTorrentClient.Protocol.Networking.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

public class HandshakeFinisher : IHandshakeFinisher
{
    private readonly HandshakeHandler _handshake;

    internal HandshakeFinisher(HandshakeHandler handshake)
    {
        _handshake = handshake;
    }


    public HandshakeData ReceivedHandshake => _handshake.ReceivedHandShake!.Value;

    public (IPeerWireReader, IMessageSender) Finish()
    {
        var (buffer, stream, writer) = _handshake.Finish();
        var messages = new BufferedMessageStream(stream, buffer);
        var reader = new PeerWireReader(messages);
        var sender = new PipedMessageSender(writer);
        return (reader, sender);
    }
}