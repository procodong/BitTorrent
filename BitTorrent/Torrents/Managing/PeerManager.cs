using BitTorrent.Errors;
using BitTorrent.Models.Application;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Managing.Events;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Peers.Errors;
using BitTorrent.Torrents.Trackers;
using BitTorrent.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;
public class PeerManager : IDisposable, IAsyncDisposable, IUpdateProvider
{
    private readonly PeerCollection _peers;
    private readonly string _peerId;
    private readonly Download _download;
    private readonly DataTransferCounter _transfered = new();
    private readonly ILogger _logger;
    private readonly ITrackerFetcher _trackerFetcher;
    private readonly ChannelReader<IPeerRegisterationEvent> _peerRegister;

    public PeerManager(string peerId, Download download, ILogger logger, ITrackerFetcher trackerFetcher)
    {
        _peerId = peerId;
        _download = download;
        _logger = logger;
        _trackerFetcher = trackerFetcher;
        var registerChannel = Channel.CreateBounded<IPeerRegisterationEvent>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        _peerRegister = registerChannel.Reader;
        _peers = new(registerChannel.Writer, logger);
    }

    private DataTransferVector Transfered => _transfered.Fetch() + _download.RecentlyTransfered;
    private DataTransferVector TransferRate => new(_download.DownloadRate, _download.UploadRate);

    private async Task AddPeerAsync(PeerWireStream stream, bool handshakeIsRead)
    {
        var eventChannel = Channel.CreateBounded<PeerRelation>(8);
        var haveChannel = Channel.CreateUnbounded<int>();
        var stats = new SharedPeerState(new(_download.Torrent.NumberOfPieces));
        int downloadWriterIndex = _download.AddPeer(haveChannel.Writer);
        var peerConnector = new PeerConnector(stats, eventChannel.Writer, new(), new());
        int index = _peers.Add(peerConnector);
        try
        {
            var handshake = new HandShake(PeerWireStream.PROTOCOL, _download.Torrent.OriginalInfoHashBytes, _peerId);
            await stream.InitializeConnectionAsync(_download.DownloadedPiecesCount != 0 ? _download.DownloadedPieces : null, handshake);
            if (!handshakeIsRead)
            {
                HandShake receivedHandshake = await stream.ReadHandShakeAsync();
                if (!receivedHandshake.InfoHash.SequenceEqual(_download.Torrent.OriginalInfoHashBytes))
                {
                    _logger.LogInformation("Encountered a peer with an invalid info hash");
                    return;
                }
            }
            await using var peer = new Peer(stream, haveChannel.Reader, eventChannel.Reader, _download, stats);
            await peer.ListenAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in peer connection: {}", ex);
        }
        finally
        {
            await _peers.QueueRemoval(index);
            _download.RemovePeer(downloadWriterIndex);
        }
    }

    private void AddPeers(IEnumerable<PeerAddress> peers)
    {
        foreach (PeerAddress peer in peers)
        {
            _ = _peers.ConnectPeerAsync(peer);
        }
    }

    public async Task ListenAsync(ChannelReader<IdentifiedPeerWireStream> trackerReceiver, TaskCompletionSource completer)
    {
        Task<IdentifiedPeerWireStream> trackerTask = trackerReceiver.ReadAsync().AsTask();
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_download.Config.PeerUpdateInterval));
        Task updateIntervalTask = updateInterval.WaitForNextTickAsync().AsTask();
        Task<IPeerRegisterationEvent> peerRegisterTask = _peerRegister.ReadAsync().AsTask();
        var rarePieceUpdateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_download.Config.RarePiecesUpdateInterval));
        Task rarePieceUpdateTask = rarePieceUpdateInterval.WaitForNextTickAsync().AsTask();
        Task trackerUpdateTask = _trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Started));
        while (true)
        {
            var ready = await Task.WhenAny(trackerTask, updateIntervalTask, peerRegisterTask, rarePieceUpdateTask, trackerUpdateTask, completer.Task);
            if (ready == trackerTask)
            {
                var (_, stream) = await trackerTask;
                _ = AddPeerAsync(stream, true);
                trackerTask = trackerReceiver.ReadAsync().AsTask();
            }
            else if (ready == updateIntervalTask)
            {
                await updateIntervalTask;
                await Update();
                updateIntervalTask = updateInterval.WaitForNextTickAsync().AsTask();
            }
            else if (ready == peerRegisterTask)
            {
                IPeerRegisterationEvent peer = await peerRegisterTask;
                if (peer is PeerRemovalEvent remove)
                {
                    _peers.Remove(remove.Index);
                }
                else if (peer is PeerAddEvent add)
                {
                    _ = AddPeerAsync(add.Stream, false);
                }
                peerRegisterTask = _peerRegister.ReadAsync().AsTask();
            }
            else if (ready == trackerUpdateTask)
            {
                if (trackerUpdateTask is Task<TrackerResponse> responseTask)
                {
                    try
                    {
                        var response = await responseTask;
                        _logger.LogInformation("Amount of peers: {} interval: {}", response.Peers.Count, response.Interval);
                        AddPeers(response.Peers);
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
            else if (ready == rarePieceUpdateTask)
            {
                await rarePieceUpdateTask;
                lock (_download)
                {
                    _download.RarestPieces.Clear();
                    _download.RarestPieces.AddRange(FindRaresPieces());
                }
                rarePieceUpdateTask = rarePieceUpdateInterval.WaitForNextTickAsync().AsTask();
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
        
        foreach (var interesting in interestingPeers)
        {
            interestingPeersLookup[interesting.Item] = true;
        }

        foreach (var unchoke in unchokedPeers)
        {
            unchokedPeersLookup[unchoke.Item] = true;
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

    public DownloadUpdate GetUpdate()
        => new(_download.Torrent.DisplayName, Transfered, TransferRate, _download.Torrent.TotalSize);
}
