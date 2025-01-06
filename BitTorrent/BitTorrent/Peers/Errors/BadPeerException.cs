using BitTorrentClient.BitTorrent.Peers.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers.Errors;
public class BadPeerException(PeerErrorReason reason) : Exception(reason.ToString())
{
    public PeerErrorReason Reason => reason;
}
