using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Reading;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public class PeerWireStream : IDisposable, IAsyncDisposable
{
    public HandshakeData ReceivedHandshake { get; }
    public IPeerWireReader Reader { get; }
    public IMessageSender Sender { get; }

    public PeerWireStream(HandshakeData receivedHandshake, IPeerWireReader reader, IMessageSender sender)
    {
        ReceivedHandshake = receivedHandshake;
        Reader = reader;
        Sender = sender;
    }

    public void Dispose()
    {
        if (Reader is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Reader is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
    }
}