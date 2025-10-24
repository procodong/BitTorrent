namespace BitTorrentClient.Core.Presentation.UdpTracker.Models;
public record class TrackerRequest(
    ReadOnlyMemory<byte> InfoHash,
    ReadOnlyMemory<byte> ClientId,
    int Port,
    long Uploaded,
    long Downloaded,
    long Left,
    TrackerEvent TrackerEvent
    );