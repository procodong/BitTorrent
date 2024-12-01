using BitTorrent.Models.Application;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Managing.Events;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Trackers;
using BitTorrent.Utils;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Managing;
public class PeerManager : IDisposable, IAsyncDisposable, IUpdateProvider
{
    private readonly PeerCollection _peers;
    private readonly string _peerId;
    private readonly Download _download;
    private readonly DataTransferCounter _transfered = new();
    private readonly ILogger _logger;
    private readonly ITrackerFetcher _trackerFetcher;
    private readonly ChannelReader<int> _peerRemovalReader;

    public PeerManager(string peerId, Download download, ChannelWriter<IdentifiedPeerWireStream> peerWriter, ILogger logger, ITrackerFetcher trackerFetcher)
    {
        _peerId = peerId;
        _download = download;
        _logger = logger;
        _trackerFetcher = trackerFetcher;
        var removalChannel = Channel.CreateBounded<int>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        _peerRemovalReader = removalChannel.Reader;
        var spawner = new PeerSpawner(download, logger, removalChannel.Writer, peerWriter, peerId);
        _peers = new(spawner, logger, download.Torrent.NumberOfPieces);
    }

    private DataTransferVector Transfered => _transfered.Fetch() + _download.RecentlyTransfered;
    private DataTransferVector TransferRate => new(_download.DownloadRate, _download.UploadRate);

    public async Task ListenAsync(ChannelReader<IdentifiedPeerWireStream> trackerReceiver, TaskCompletionSource completer)
    {
        Task<IdentifiedPeerWireStream> trackerTask = trackerReceiver.ReadAsync().AsTask();
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_download.Config.PeerUpdateInterval));
        Task updateIntervalTask = updateInterval.WaitForNextTickAsync().AsTask();
        Task<int> peerRemovalTask = _peerRemovalReader.ReadAsync().AsTask();
        Task trackerUpdateTask = _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Started));
        while (true)
        {
            var ready = await Task.WhenAny(trackerTask, updateIntervalTask, peerRemovalTask, trackerUpdateTask, completer.Task);
            if (ready == trackerTask)
            {
                var (id, stream) = await trackerTask;
                _peers.AddPeer(id, stream);
                trackerTask = trackerReceiver.ReadAsync().AsTask();
            }
            else if (ready == updateIntervalTask)
            {
                await updateIntervalTask;
                await Update();
                updateIntervalTask = updateInterval.WaitForNextTickAsync().AsTask();
            }
            else if (ready == peerRemovalTask)
            {
                int peer = await peerRemovalTask;
                _peers.Remove(peer);
                peerRemovalTask = _peerRemovalReader.ReadAsync().AsTask();
            }
            else if (ready == trackerUpdateTask)
            {
                if (trackerUpdateTask is Task<TrackerResponse> responseTask)
                {
                    try
                    {
                        var response = await responseTask;
                        _logger.LogInformation("Amount of peers: {} interval: {}", response.Peers.Count, response.Interval);
                        _peers.Connect(response.Peers);
                        trackerUpdateTask = Task.Delay(response.Interval * 1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("{Message}", ex);
                        trackerUpdateTask = Task.Delay(5000);
                    }
                }
                else
                {
                    trackerUpdateTask = _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.None));
                }
            }
            else if (ready == completer.Task)
            {
                return;
            }
        }
    }

    private async Task Update()
    {
        try
        {
            var (interesting, unChoked) = GetPeerRelations();
            await UpdateRelations(interesting, unChoked);
            var recent = _download.ResetRecentTransfer();
            _transfered.FetchAdd(recent);
            if (_download.RarestPieces.Count == 0)
            {
                var pieces = FindRaresPieces();
                lock (_download)
                {
                    _download.RarestPieces.Clear();
                    _download.RarestPieces.AddRange(pieces);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{}", ex);
        }
    }

    private TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent)
        => new(_download.Torrent.OriginalInfoHashBytes, _peerId, _transfered.AsVector(), _download.Torrent.TotalSize - _transfered.Downloaded, trackerEvent);

    private (BitArray Interesting, BitArray UnChoked) GetPeerRelations()
    {
        var interestingPeers = new SumPriorityStack<int>(_download.Config.TargetDownload);
        var unchokedPeers = new SumPriorityStack<int>(_download.FinishedDownloading ? _download.Config.TargetUploadSeeding : _download.Config.TargetUpload);

        foreach (var (index, peer) in _peers.Indexed())
        {
            bool interested = peer.Data.Relation.Interested;
            bool choked = peer.Data.Relation.Choked;
            var stats = new DataTransferVector(
                choked ? peer.LastUnchokedStats.Downloaded : Interlocked.Read(ref peer.Data.Stats.Downloaded), 
                !interested ? peer.LastUnchokedStats.Uploaded : Interlocked.Read(ref peer.Data.Stats.Uploaded)
                );
            var statChange = (stats - peer.LastStatistics) / _download.SecondsSinceTimerReset;
            unchokedPeers.Include(index, (int)statChange.Upload, (int)statChange.Upload);
            interestingPeers.Include(index, (int)statChange.Download, (int)statChange.Download);
            peer.LastStatistics = stats;
        }
        var interestingPeersLookup = new BitArray(_peers.Count);
        var unchokedPeersLookup = new BitArray(_peers.Count);
        
        foreach (var (index, _) in interestingPeers)
        {
            interestingPeersLookup[index] = true;
        }

        foreach (var (index, _) in unchokedPeers)
        {
            unchokedPeersLookup[index] = true;
        }
        return (interestingPeersLookup, unchokedPeersLookup);
    }

    private async Task UpdateRelations(BitArray interestingPeers, BitArray unchokedPeers)
    {
        foreach (var (index, peer) in _peers.Indexed())
        {
            bool interesting = interestingPeers[index];
            bool unChoked = unchokedPeers[index];
            if (!interesting)
            {
                peer.LastUnchokedStats.Uploaded = peer.LastStatistics.Download;
            }
            if (!unChoked)
            {
                peer.LastUnchokedStats.Downloaded = peer.LastStatistics.Download;
            }
            await peer.RelationEventWriter.WriteAsync(new(interesting, !unChoked));
        }
    }

    private IEnumerable<int> FindRaresPieces()
    {
        int rareCount = int.Min(_download.Torrent.NumberOfPieces / 10, _download.Config.MaxRarePieceCount);
        var comparer = Comparer<(int Index, int Count)>.Create((v1, v2) => v1.Count - v2.Count);
        var rarestPieceStack = new PriorityStack<(int Index, int Count)>(rareCount, comparer);
        foreach (var piece in PieceCounts().Indexed())
        {
            rarestPieceStack.Include(piece);
        }
        return rarestPieceStack.Select(v => v.Index);
    }

    private IEnumerable<int> PieceCounts()
    {
        for (int i = 0; i < _download.Torrent.NumberOfPieces; i++)
        {
            int count = _peers.Select(peer => peer.Data.OwnedPieces[i] ? 1 : 0).Sum();
            yield return count;
        }
    }
    public DownloadUpdate GetUpdate()
        => new(_download.Torrent.DisplayName, Transfered, TransferRate, _download.Torrent.TotalSize);

    public void Dispose()
    {
        foreach (var peer in _peers)
        {
            peer.RelationEventWriter.Complete();
            peer.Data.Completion.Task.GetAwaiter().GetResult();
        }
        _download.Dispose();
        _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Stopped));
        
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var peer in _peers)
        {
            peer.RelationEventWriter.Complete();
            await peer.Data.Completion.Task;
        }
        await _download.DisposeAsync();
        await _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Stopped));
    }

}
