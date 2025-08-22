namespace BitTorrentClient.Models.Peers;
public readonly record struct PeerRelation(bool Interested = false, bool Choked = true);