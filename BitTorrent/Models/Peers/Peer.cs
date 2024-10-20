namespace BitTorrent.Models.Peers;
public readonly record struct Peer(string Id, string Ip, int Port);