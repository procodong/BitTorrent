using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Reading;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

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

    public  ValueTask DisposeAsync()
    {  
        return Reader.DisposeAsync();
    }
}