using BitTorrent.Models.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Peers;
public interface IPeerEventHandler
{
    Task OnChokeAsync();
    Task OnUnchokedAsync();
    Task OnInterestedAsync();
    Task OnNotInterestedAsync();
    Task OnHaveAsync(int piece);
    Task OnBitfieldAsync(BitArray bitfield);
    Task OnRequestAsync(PieceRequest request);
    Task OnPieceAsync(Piece piece);
    Task OnCancelAsync(PieceRequest request);
    Task OnPortAsync(ushort port);
}
