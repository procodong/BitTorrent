using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

internal class DownloadState
{
    public LazyBitArray DownloadedPieces { get; }
    public DataTransferCounter DataTransfer { get; }
    public Download Download { get; }
    public DataTransferCounter RecentDataTransfer { get; }
    public DownloadExecutionState ExecutionState { get; set; }

    private readonly AtomicWatch _recentTransferWatch;
    public DownloadState(Download download)
    {
        Download = download;
        DataTransfer = new();
        RecentDataTransfer = new();
        DownloadedPieces = new(download.Data.PieceCount);
        _recentTransferWatch = new();
    }

    public void ResetRecentTransfer()
    {
        _recentTransferWatch.Reset();
        RecentDataTransfer.FetchReplace(default);
    }
    public long ElapsedSinceRecentReset => _recentTransferWatch.Elapsed;
    public DataTransferVector TransferRate => RecentDataTransfer.Fetch() / ElapsedSinceRecentReset;

    public DownloadUpdate GetUpdate()
    {
        return new(Download.Data.Name, DataTransfer.Fetch(), TransferRate, Download.Data.Size, ExecutionState, Download.Data.InfoHash);
    }
}