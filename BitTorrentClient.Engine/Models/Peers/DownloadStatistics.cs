using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Models.Peers;

public readonly record struct DownloadStatistics(DataTransferVector TransferRate, DataTransferVector MaxTransferRate, int PeerCount);