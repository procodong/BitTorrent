using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Models.Application;
public readonly record struct DownloadUpdate(string DownloadName, DataTransferVector Transfer, DataTransferVector TransferRate, long Size, DownloadExecutionState ExecutionState);
