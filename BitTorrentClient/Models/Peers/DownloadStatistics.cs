using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Models.Peers;

public readonly record struct DownloadStatistics(DataTransferVector TransferRate, DataTransferVector MaxTransferRate, int PeerCount);