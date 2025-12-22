namespace BitTorrentClient.Core.Presentation.PeerWire.Models;
public readonly record struct Handshake(string Protocol, ulong Extensions, ReadOnlyMemory<byte> InfoHash, ReadOnlyMemory<byte> PeerId);