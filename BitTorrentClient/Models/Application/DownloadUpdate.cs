using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Models.Application;

public readonly record struct DownloadUpdate(string DownloadName, DataTransferVector Transfer, DataTransferVector TransferRate, long Size, DownloadExecutionState ExecutionState, ReadOnlyMemory<byte> Identifier)
{
    public double Progress => Transfer.Download / Size * 100;
}
