using BitTorrentClient.Core.Presentation.PeerWire.Models;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Core.Transport.PeerWire.Handshakes.Interface;

public interface IHandshakeHandler : IDisposable, IAsyncDisposable
{
    public HandshakeData? ReceivedHandshake { get; }
    
    PeerWireStream Finish();
    Task ReadHandShakeAsync(CancellationToken cancellationToken = default);
    Task SendBitfieldAsync(LazyBitArray bitfield, CancellationToken cancellationToken = default);
    Task SendHandShakeAsync(HandshakeData handshake, CancellationToken cancellationToken = default);
}