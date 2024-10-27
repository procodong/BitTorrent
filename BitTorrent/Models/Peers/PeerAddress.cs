namespace BitTorrent.Models.Peers;
public readonly record struct PeerAddress(string Id, string Ip, int Port);