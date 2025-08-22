using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Api.Information;

public readonly record struct DownloadUpdate(string DownloadName, DataTransferVector Transfer, DataTransferVector TransferRate, long Size, DownloadExecutionState ExecutionState, ReadOnlyMemory<byte> Identifier)
{
    public double Progress => (double)Transfer.Download / Size * 100;
}
