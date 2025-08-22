using System.Net;

namespace BitTorrentClient.Models.Trackers;
public readonly record struct UdpTrackerData(
    int Interval,
    int Complete,
    int Incomplete,
    int PeerCount,
    IPEndPoint[] Peers);