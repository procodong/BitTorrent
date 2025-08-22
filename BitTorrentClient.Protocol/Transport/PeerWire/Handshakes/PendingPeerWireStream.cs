using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes.Interface;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

public readonly struct PendingPeerWireStream<TPhase> : IAsyncDisposable, IDisposable
    where TPhase : IInitializationPhase
{
    internal IHandshakeHandler Handler { get; }

    internal PendingPeerWireStream(IHandshakeHandler handler)
    {
        Handler = handler;
    }

    public ValueTask DisposeAsync()
    {
        return Handler.DisposeAsync();
    }

    public void Dispose()
    {
        Handler.Dispose();
    }
}