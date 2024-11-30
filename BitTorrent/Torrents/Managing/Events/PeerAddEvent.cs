using BitTorrent.Torrents.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing.Events;
public readonly record struct PeerAddEvent(PeerWireStream Stream) : IPeerRegisterationEvent;