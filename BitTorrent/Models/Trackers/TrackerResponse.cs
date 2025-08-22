using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Trackers;
public record class TrackerResponse(
    int Interval,
    int? MinInterval,
    int Complete,
    int Incomplete,
    PeerAddress[] Peers,
    string? Warning
    );