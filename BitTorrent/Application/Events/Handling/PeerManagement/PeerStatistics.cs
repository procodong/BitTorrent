using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public readonly record struct PeerStatistics(DataTransferVector DataTransfer, PeerRelation PeerRelation, PeerRelation ClientRelation);