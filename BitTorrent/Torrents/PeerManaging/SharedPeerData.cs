using BitTorrent.Models.Peers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public record class SharedPeerData(PeerStatistics Stats, BitArray OwnedPieces);