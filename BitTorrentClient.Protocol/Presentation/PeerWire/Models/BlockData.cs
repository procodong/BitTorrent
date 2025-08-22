namespace BitTorrentClient.Protocol.Presentation.PeerWire.Models;
public readonly record struct BlockData(BlockRequest Request, Stream Stream);