namespace BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
public record class TrackerRequest(
    ReadOnlyMemory<byte> InfoHash,
    string ClientId,
    int Port,
    long Uploaded,
    long Downloaded,
    long Left,
    TrackerEvent TrackerEvent
    );