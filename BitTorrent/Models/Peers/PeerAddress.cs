using System.Net;

namespace BitTorrent.Models.Peers;
public readonly record struct PeerAddress(IPAddress Ip, int Port);