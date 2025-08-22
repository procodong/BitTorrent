using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Listening.Peers;
public interface IPeerEventHandler
{
    Task OnPeerRelationAsync(RelationUpdate relation, CancellationToken cancellationToken = default);
    Task OnClientRelationAsync(DataTransferVector transferLimit, CancellationToken cancellationToken = default);
    Task OnPeerHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnClientHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnBitfieldAsync(Stream bitfield, CancellationToken cancellationToken = default);
    Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default);
    Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default);
    Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default);
}
