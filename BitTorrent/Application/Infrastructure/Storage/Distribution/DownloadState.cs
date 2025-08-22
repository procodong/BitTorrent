using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;

public class DownloadState
{
    public LazyBitArray DownloadedPieces { get; }
    public SlotMap<ChannelWriter<int>> Peers { get; }
    public DataTransferCounter DataTransfer { get; }
    public DataTransferCounter RecentDataTransfer { get; }
    public DownloadStorage Storage { get; }
    public Download Download { get; }
    public DownloadState(Download download, DownloadStorage storage)
    {
        Download = download;
        Peers = [];
        DataTransfer = new();
        RecentDataTransfer = new();
        Storage = storage;
        DownloadedPieces = new(download.Torrent.NumberOfPieces);
    }
    
    private long _recentTransferReset;

    public void ResetRecentTransfer()
    {
        Interlocked.Exchange(ref _recentTransferReset, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        RecentDataTransfer.FetchReplace(default);
    }
    public long ElapsedSinceRecentReset => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Interlocked.Read(ref _recentTransferReset);
    public DataTransferVector TransferRate => RecentDataTransfer.Fetch() / ElapsedSinceRecentReset;
}