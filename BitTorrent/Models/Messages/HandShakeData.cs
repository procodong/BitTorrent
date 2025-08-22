namespace BitTorrentClient.Models.Messages;

public readonly record struct HandShakeData(ulong Extensions, byte[] InfoHash, byte[] PeerId);