using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers.Errors;
public enum PeerErrorReason
{
    InvalidRequest,
    InvalidPiece,
    InvalidProtocol,
    InvalidPacketSize
}
