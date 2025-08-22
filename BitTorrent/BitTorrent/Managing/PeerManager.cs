using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.BitTorrent.Downloads;
using BitTorrentClient.BitTorrent.Trackers;
using BitTorrentClient.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Threading.Channels;
using BitTorrentClient.BitTorrent.Peers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;

namespace BitTorrentClient.BitTorrent.Managing;
public class PeerManager : IDisposable, IAsyncDisposable
{
    private readonly PeerCollection _peers;
    private readonly string _peerId;
    private readonly Download _download;
    private readonly DataTransferCounter _transfered = new();
    private readonly ILogger _logger;
    private readonly ITrackerFetcher _trackerFetcher;
    private int _updateTick;
    private DataTransferVector Transfered => _transfered.Fetch() + _download.RecentlyTransfered;
    private DataTransferVector TransferRate => new(_download.DownloadRate, _download.UploadRate);

    public PeerManager(string peerId, Download download, PeerCollection peers, ILogger logger, ITrackerFetcher trackerFetcher)
    {
        _peerId = peerId;
        _download = download;
        _logger = logger;
        _trackerFetcher = trackerFetcher;
        _peers = peers;
    }

    public async Task ListenAsync(ChannelReader<PeerHandshaker> peerReader, ChannelReader<int?> peerRemoveReader, CancellationToken cancellationToken = default)
    {
        var events = new PeerManagerEventHandler(peerRemoveReader, peerReader, _trackerFetcher, _download.Config.PeerUpdateInterval, GetTrackerUpdate)
        {
            Update = Update,
            PeerAddition = _peers.Add,
            PeerRemoval = _peers.RemoveAsync,
            TrackerResponse = (response) => _peers.Update(response.Peers),
            Error = (err) => _logger.LogError("Peer manager", err)
        };
        await events.ListenAsync(cancellationToken);
    }

    private async Task Update()
    {
        var (interesting, unChoked) = GetPeerRelations();
        await UpdateRelations(interesting, unChoked);
        if (_updateTick % _download.Config.TransferRateResetInterval == 0)
        {
            var recent = _download.ResetRecentTransfer();
            _transfered.AtomicAdd(recent);
        }
        unchecked
        {
            _updateTick++;
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
            bool interested = peer.State.Relation.Interested;
            bool choked = peer.State.Relation.Choked;
            var stats = new DataTransferVector(
                choked ? peer.LastUnchokedStats.Download : peer.State.DataTransfer.Downloaded, 
                !interested ? peer.LastUnchokedStats.Upload : peer.State.DataTransfer.Uploaded
                );
            var statChange = (stats - peer.LastStatistics) / _download.SecondsSinceTimerReset;
            unchokedPeers.Include(index, (int)statChange.Upload + (int)statChange.Download, (int)statChange.Upload);
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
            peer.LastUnchokedStats = peer.LastUnchokedStats with
            {
                Upload = interesting ? peer.LastUnchokedStats.Upload : peer.LastStatistics.Download,
                Download = unChoked ? peer.LastUnchokedStats.Download : peer.LastStatistics.Download
            };
            if (interesting != peer.State.Relation.Interested || !unChoked != peer.State.Relation.Choked)
            {
                await peer.RelationEventWriter.WriteAsync(new(interesting, !unChoked));
            }
        }
    }

    private IEnumerable<int> FindRaresPieces()
    {
        int rareCount = int.Min(_download.Torrent.NumberOfPieces / 10, _download.Config.MaxRarePieceCount);
        var comparer = Comparer<(int Index, int Count)>.Create((v1, v2) => v2.Count - v1.Count);
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
            int count = _peers.Select(peer => peer.State.OwnedPieces[i] ? 1 : 0).Sum();
            yield return count;
        }
    }
    public DownloadUpdate GetUpdate()
        => new(_download.Torrent.DisplayName, Transfered, TransferRate, _download.Torrent.TotalSize);

    public void Dispose()
    {
        foreach (var peer in _peers)
        {
            peer.Canceller.Cancel();
        }
        _download.Dispose();
        _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Stopped)).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var peer in _peers)
        {
            await peer.Canceller.CancelAsync();
        }
        await _download.DisposeAsync();
        await _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Stopped));
    }

}
