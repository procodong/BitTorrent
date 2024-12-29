using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Peers;
public readonly record struct PeerRelation(bool Interested = false, bool Choked = true);