namespace BitTorrent.Models.Tracker;
public record class TrackerRequest(
    byte[] InfoHash,
    string ClientId,
    int Port,
    long Uploaded,
    long Downloaded,
    long Left,
    TrackerEvent? TrackerEvent
    );