using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Events.Handling.Interface;
public interface IPeerEventHandler
{
    Task OnPeerRelationAsync(RelationUpdate relation, CancellationToken cancellationToken = default);
    Task OnClientRelationAsync(DataTransferVector transferLimit, CancellationToken cancellationToken = default);
    Task OnPeerHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnClientHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnBitfieldAsync(Stream bitfield, CancellationToken cancellationToken = default);
    Task OnRequestAsync(BlockRequest request, CancellationToken cancellationToken = default);
    Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default);
    Task OnCancelAsync(BlockRequest request, CancellationToken cancellationToken = default);
}
