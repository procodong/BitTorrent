using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

public class DownloadState
{
    public LazyBitArray DownloadedPieces { get; }
    public DataTransferCounter DataTransfer { get; }
    public Download Download { get; }
    public DataTransferCounter RecentDataTransfer { get; }
    public DownloadExecutionState ExectutionState { get; set; }

    private readonly AtomicWatch _recentTransferWatch;
    public DownloadState(Download download)
    {
        Download = download;
        DataTransfer = new();
        RecentDataTransfer = new();
        DownloadedPieces = new(download.Torrent.NumberOfPieces);
        _recentTransferWatch = new();
    }

    public void ResetRecentTransfer()
    {
        _recentTransferWatch.Reset();
        RecentDataTransfer.FetchReplace(default);
    }
    public long ElapsedSinceRecentReset => _recentTransferWatch.Elapsed;
    public DataTransferVector TransferRate => RecentDataTransfer.Fetch() / ElapsedSinceRecentReset;
}