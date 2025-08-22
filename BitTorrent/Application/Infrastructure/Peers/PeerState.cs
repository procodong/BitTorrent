using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.Peers;
public class PeerState(LazyBitArray ownedPieces)
{
    public PeerRelation RelationToMe = new();
    public PeerRelation Relation = new();
    public LazyBitArray OwnedPieces = ownedPieces;
    public DataTransferCounter DataTransfer = new();
    public TaskCompletionSource Completion = new();
}