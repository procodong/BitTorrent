using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
public record struct TrackerUpdate(
    byte[] InfoHash,
    string ClientId,
    DataTransferVector DataTransfer,
    long Left,
    TrackerEvent TrackerEvent
    );