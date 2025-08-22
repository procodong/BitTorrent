namespace BitTorrentClient.Models.Messages;
public readonly record struct BlockData(BlockRequest Request, Stream Stream);