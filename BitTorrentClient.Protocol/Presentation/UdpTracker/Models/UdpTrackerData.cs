using System.Net;

namespace BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
public readonly record struct UdpTrackerData(
    int Interval,
    int Complete,
    int Incomplete,
    int PeerCount,
    IPEndPoint[] Peers);