namespace BitTorrentClient.Models.Peers;

public readonly record struct DownloadStatistics(DataTransferVector TransferRate, int PeerCount);