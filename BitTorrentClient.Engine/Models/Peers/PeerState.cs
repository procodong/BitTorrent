using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Models.Peers;
public class PeerState(LazyBitArray ownedPieces, DataTransferVector transferLimit)
{
    public PeerRelation RelationToMe { get; set; }
    public PeerRelation Relation { get; set; }
    public LazyBitArray OwnedPieces { get; set; } = ownedPieces;
    public DataTransferCounter TransferLimit { get; } = new(transferLimit);
    public DataTransferCounter DataTransfer { get; } = new();
}