using BitTorrentClient.Core.Transport.PeerWire.Connecting.Interface;

namespace BitTorrentClient.Core.Presentation.UdpTracker.Models;
public record class TrackerResponse(
    TimeSpan Interval,
    TimeSpan? MinInterval,
    int Complete,
    int Incomplete,
    IEnumerable<IPeerConnector> Peers,
    string? Warning
    );