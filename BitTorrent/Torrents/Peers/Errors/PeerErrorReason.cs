using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Peers.Errors;
public enum PeerErrorReason
{
    InvalidInfoHash,
    InvalidPacketSize,
    InvalidProtocol,
    InvalidPiece,
    InvalidRequest
}
