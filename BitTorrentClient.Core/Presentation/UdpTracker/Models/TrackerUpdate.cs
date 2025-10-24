using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Core.Presentation.UdpTracker.Models;
public record struct TrackerUpdate(
    ReadOnlyMemory<byte> InfoHash,
    ReadOnlyMemory<byte> ClientId,
    DataTransferVector DataTransfer,
    long Left,
    TrackerEvent TrackerEvent
    );