using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Models.Peers;
public class PeerState(LazyBitArray ownedPieces, DataTransferVector transferLimit)
{
    public PeerRelation RelationToMe { get; set; } = new();
    public PeerRelation Relation { get; set; } = new();
    public LazyBitArray OwnedPieces { get; set; } = ownedPieces;
    public DataTransferCounter TransferLimit { get; } = new(transferLimit);
    public DataTransferCounter DataTransfer { get; } = new();
}