namespace BitTorrentClient.Models.Messages;

public readonly record struct HandshakeData(ulong Extensions, byte[] InfoHash, byte[] PeerId);