using BitTorrentClient.Models.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTorrentClient.Helpers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.EventListening.Peers;
public interface IPeerEventHandler
{
    Task OnPeerRelationAsync(Relation relation, CancellationToken cancellationToken = default);
    Task OnClientRelationAsync(PeerRelation relation, CancellationToken cancellationToken = default);
    Task OnPeerHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnClientHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnBitfieldAsync(Stream bitfield, CancellationToken cancellationToken = default);
    Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default);
    Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default);
    Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default);
}
