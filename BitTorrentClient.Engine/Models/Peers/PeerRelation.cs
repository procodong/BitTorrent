namespace BitTorrentClient.Engine.Models.Peers;
public readonly record struct PeerRelation(bool Interested = false, bool Choked = true);