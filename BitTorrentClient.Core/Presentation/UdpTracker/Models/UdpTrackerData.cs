using System.Net;

namespace BitTorrentClient.Core.Presentation.UdpTracker.Models;
public readonly record struct UdpTrackerData(
    int Interval,
    int Complete,
    int Incomplete,
    int PeerCount,
    IPEndPoint[] Peers);