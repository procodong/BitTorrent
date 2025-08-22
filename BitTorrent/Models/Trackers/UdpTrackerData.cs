using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Trackers;
public readonly record struct UdpTrackerData(
    int Interval,
    int Complete,
    int Incomplete,
    int PeerCount,
    IEnumerable<PeerAddress> Peers);