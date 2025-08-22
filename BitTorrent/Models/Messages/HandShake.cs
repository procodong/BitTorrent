namespace BitTorrentClient.Models.Messages;
public readonly record struct HandShake(string Protocol, ulong Extensions, byte[] InfoHash, byte[] PeerId);