using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public readonly record struct PeerStatistics(DataTransferVector DataTransferPerSecond, PeerRelation PeerRelation, PeerRelation ClientRelation);