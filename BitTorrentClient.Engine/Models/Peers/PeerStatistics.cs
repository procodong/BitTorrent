using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Models.Peers;
public readonly record struct PeerStatistics(DataTransferVector DataTransfer, PeerRelation PeerRelation, PeerRelation ClientRelation);