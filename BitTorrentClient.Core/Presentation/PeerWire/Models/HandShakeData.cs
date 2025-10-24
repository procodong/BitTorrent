namespace BitTorrentClient.Core.Presentation.PeerWire.Models;

public readonly record struct HandshakeData(ulong Extensions, ReadOnlyMemory<byte> InfoHash, ReadOnlyMemory<byte> PeerId);