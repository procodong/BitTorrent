namespace BitTorrentClient.Models.Messages;
public readonly record struct BlockData(PieceRequest Request, Stream Stream);