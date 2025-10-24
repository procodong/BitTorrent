using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Core.Transport.PeerWire.Handshakes;

public static class PendingPeerWireStreamExt
{
    public static async Task<PendingPeerWireStream<ReadDataPhase>> SendDataAsync(this PendingPeerWireStream<InitialSendDataPhase> stream, HandshakeData handshake,
        LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        await stream.Handler.SendHandShakeAsync(handshake, cancellationToken);
        await stream.Handler.SendBitfieldAsync(bitfield, cancellationToken);
        return new(stream.Handler);
    }
    
    public static async Task<PeerWireStream> SendDataAsync(this PendingPeerWireStream<SendDataPhase> stream, HandshakeData handshake,
        LazyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        await stream.Handler.SendHandShakeAsync(handshake, cancellationToken);
        await stream.Handler.SendBitfieldAsync(bitfield, cancellationToken);
        return stream.Handler.Finish();
    }

    public static async Task<PendingPeerWireStream<SendDataPhase>> ReadDataAsync(this PendingPeerWireStream<InitialReadDataPhase> stream, CancellationToken cancellationToken = default)
    {
        await stream.Handler.ReadHandShakeAsync(cancellationToken);
        return new(stream.Handler);
    }

    public static async Task<PeerWireStream> ReadDataAsync(this PendingPeerWireStream<ReadDataPhase> stream, CancellationToken cancellationToken = default)
    {
        await stream.Handler.ReadHandShakeAsync(cancellationToken);
        return stream.Handler.Finish();
    }

    public static HandshakeData GetReceivedHandshake(this PendingPeerWireStream<SendDataPhase> stream)
    {
        return stream.Handler.ReceivedHandshake!.Value;
    }
}