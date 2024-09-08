using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models;
public record class TrackerResponse(
    int Interval,
    int? MinInterval,
    string TrackerId,
    int Complete,
    int Incomplete,
    List<Peer> Peers,
    string? Warning
    );