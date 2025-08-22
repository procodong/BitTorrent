using BitTorrentClient.Models.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTorrentClient.Utils;

namespace BitTorrentClient.BitTorrent.Peers.Connections;
public interface IPeerEventHandler
{
    Task OnChokeAsync(CancellationToken cancellationToken = default);
    Task OnUnChokedAsync(CancellationToken cancellationToken = default);
    Task OnInterestedAsync(CancellationToken cancellationToken = default);
    Task OnNotInterestedAsync(CancellationToken cancellationToken = default);
    Task OnHaveAsync(int piece, CancellationToken cancellationToken = default);
    Task OnBitfieldAsync(ZeroCopyBitArray bitfield, CancellationToken cancellationToken = default);
    Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default);
    Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default);
    Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default);
}
