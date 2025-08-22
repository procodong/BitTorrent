using BitTorrentClient.Protocol.Transport.PeerWire.Connecting;

namespace BitTorrentClient.Models.Trackers;
public record class TrackerResponse(
    int Interval,
    int? MinInterval,
    int Complete,
    int Incomplete,
    IEnumerable<IPeerConnector> Peers,
    string? Warning
    );