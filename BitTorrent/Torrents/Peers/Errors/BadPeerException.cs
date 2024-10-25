using BitTorrent.Torrents.Peers.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Errors;
public class BadPeerException(PeerErrorReason error) : Exception
{
    public readonly PeerErrorReason Reason = error;
}
