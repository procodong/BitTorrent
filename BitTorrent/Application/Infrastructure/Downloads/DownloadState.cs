using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

public class DownloadState
{
    public LazyBitArray DownloadedPieces { get; }
    public SlotMap<ChannelWriter<int>> Peers { get; }
    public DataTransferCounter DataTransfer { get; }
    public DownloadStorage Storage { get; }
    public Download Download { get; }
    public DataTransferCounter RecentDataTransfer { get; }

    private readonly AtomicWatch _recentTransferWatch;
    public DownloadState(Download download, DownloadStorage storage)
    {
        Download = download;
        Peers = [];
        DataTransfer = new();
        RecentDataTransfer = new();
        Storage = storage;
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