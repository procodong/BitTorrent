namespace BitTorrentClient.Protocol.Presentation.PeerWire.Models;
public readonly record struct HandShake(string Protocol, ulong Extensions, byte[] InfoHash, byte[] PeerId);