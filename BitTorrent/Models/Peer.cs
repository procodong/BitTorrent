namespace BitTorrent.Models;
public readonly record struct Peer(string Id, string Ip, int Port);