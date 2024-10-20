namespace BitTorrent.Models.Tracker;
public record class TrackerRequest(
    byte[] InfoHash,
    string ClientId,
    int Port,
    ulong Uploaded,
    ulong Downloaded,
    ulong Left,
    TrackerEvent TrackerEvent
    );