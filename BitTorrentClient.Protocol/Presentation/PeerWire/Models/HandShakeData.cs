namespace BitTorrentClient.Protocol.Presentation.PeerWire.Models;

public readonly record struct HandshakeData(ulong Extensions, byte[] InfoHash, byte[] PeerId);