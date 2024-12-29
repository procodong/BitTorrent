using System.Net;

namespace BitTorrentClient.Models.Peers;
public readonly record struct PeerAddress(IPAddress Ip, int Port);