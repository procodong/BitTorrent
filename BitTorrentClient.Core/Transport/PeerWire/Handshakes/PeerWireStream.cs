using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Core.Transport.PeerWire.Reading;
using BitTorrentClient.Core.Transport.PeerWire.Sending;

namespace BitTorrentClient.Core.Transport.PeerWire.Handshakes;

public class PeerWireStream : IDisposable, IAsyncDisposable
{
    public HandshakeData ReceivedHandshake { get; }
    public IPeerWireReader Reader { get; }
    public IPeerWireWriter Writer { get; }

    public PeerWireStream(HandshakeData receivedHandshake, IPeerWireReader reader, IPeerWireWriter writer)
    {
        ReceivedHandshake = receivedHandshake;
        Reader = reader;
        Writer = writer;
    }

    public void Dispose()
    {
        Reader.Dispose();
    }

    public ValueTask DisposeAsync()
    {  
        return Reader.DisposeAsync();
    }
}