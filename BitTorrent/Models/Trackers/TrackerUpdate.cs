using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Models.Trackers;
public record struct TrackerUpdate(
    byte[] InfoHash,
    string ClientId,
    DataTransferVector DataTransfer,
    long Left,
    TrackerEvent TrackerEvent
    );