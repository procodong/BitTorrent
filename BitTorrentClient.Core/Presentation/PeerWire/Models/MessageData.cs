using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Core.Presentation.PeerWire.Models;

public readonly record struct MessageData(BlockRequest Request, int PieceIndex, BlockData Block, ZeroCopyBitArray Bitfield);