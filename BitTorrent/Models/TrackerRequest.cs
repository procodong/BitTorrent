namespace BitTorrent.Models;
public record class TrackerRequest(
    byte[] InfoHash, 
    string ClientId, 
    int Port, 
    ulong Uploaded, 
    ulong Downloaded, 
    ulong Left, 
    TrackerEvent TrackerEvent
    );