namespace BitTorrentClient.Models.Peers;

public record struct DownloadStatistics(DataTransferVector TransferRate, int PeerCount);