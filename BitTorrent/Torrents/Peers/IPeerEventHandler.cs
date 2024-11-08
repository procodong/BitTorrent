using System;
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
    
}
