using System.Net;

namespace BitTorrent.Models.Peers;
public readonly record struct PeerAddress(string Id, IPAddress Ip, int Port);