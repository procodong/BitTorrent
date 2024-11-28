using BitTorrent.Torrents.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Peers;
public readonly record struct IdentifiedPeerWireStream(string PeerId, PeerWireStream Stream);