using BitTorrent.Errors;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Downloads;
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
public class PeerManager(string peerId, Download download, ILogger logger) : IDisposable, IAsyncDisposable
{
    private readonly SlotMap<PeerConnector> _peers = [];
    private readonly HashSet<string> _peerIds = [];
    private readonly string _peerId = peerId;
    private readonly Download _download = download;
    private readonly DataTransferCounter _transfered = new();
    private readonly ILogger _logger = logger;
    private readonly Channel<int> _peerDeleter = Channel.CreateBounded<int>(new BoundedChannelOptions(8)
    {
        SingleWriter = false
    });

    public DataTransferVector Transfered => _transfered.Fetch() + _download.RecentlyTransfered;
    public DataTransferVector TransferRate => new(_download.DownloadRate, _download.UploadRate);
    public long Size => _download.Torrent.TotalSize;
    public byte[] InfoHash => _download.Torrent.OriginalInfoHashBytes;

    public void AddPeer(string peerId, PeerWireStream stream)
    {
        var eventChannel = Channel.CreateBounded<PeerRelation>(8);
        var haveChannel = Channel.CreateUnbounded<int>();
        var stats = new SharedPeerData(new(_download.Torrent.NumberOfPieces));
        int downloadWriterIndex = _download.AddPeer(haveChannel.Writer);
        var peer = new PeerConnector(stats, eventChannel.Writer, new(), new(), peerId);
        _peerIds.Add(peerId);
        int index = _peers.Add(peer);
        Task.Run(async () =>
        {
            try
            {
                var handshake = new HandShake(PeerWireStream.PROTOCOL, _download.Torrent.OriginalInfoHashBytes, _peerId);
                await stream.InitializeConnectionAsync(_download.DownloadedPiecesCount != 0 ? _download.DownloadedPieces : null, handshake);
                await using var peer = new Peer(stream, haveChannel.Reader, eventChannel.Reader, _download, stats);
                await peer.ListenAsync();
            }
            finally
            {
                await _peerDeleter.Writer.WriteAsync(index);
                _download.RemovePeer(downloadWriterIndex);
            }
        });
    }

    private void RemovePeer(int index)
    {
        var peer = _peers[index];
        peer.RelationEventWriter.Complete();
        _peerIds.Remove(peer.PeerId);
        _peers.Remove(index);
    }

    public async Task UpdatePeers(List<PeerAddress> peers)
    {
        foreach (PeerAddress peer in peers)
        {
            try
            {
                var connection = new TcpClient
                {
                    ReceiveTimeout = _download.Config.ReceiveTimeout,
                };
                await connection.ConnectAsync(peer.Ip, peer.Port);
                var stream = new NetworkStream(connection.Client, true);
                var peerStream = new PeerWireStream(stream);
                HandShake handshake = await peerStream.ReadHandShakeAsync();
                if (!handshake.InfoHash.SequenceEqual(_download.Torrent.OriginalInfoHashBytes))
                {
                    continue;
                }
                AddPeer(handshake.PeerId, peerStream);
            }
            catch (Exception exc)
            {
                _logger.LogError("Error connecting to peer: {Error}", exc);
                continue;
            }
        }
    }

    public async Task ListenAsync(ChannelReader<IdentifiedPeerWireStream> trackerReceiver, ITrackerFetcher trackerFetcher)
    {
        Task<IdentifiedPeerWireStream> trackerTask = trackerReceiver.ReadAsync().AsTask();
        Task updateIntervalTask = Task.Delay(_download.Config.PeerUpdateInterval);
        Task<int> peerDeletionTask = _peerDeleter.Reader.ReadAsync().AsTask();
        Task trackerUpdateTask = trackerFetcher.FetchAsync(GetTrackerUpdate(TrackerEvent.Started));
        var rarePieceUpdateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_download.Config.RarePiecesUpdateInterval));
        Task rarePieceUpdateTask = rarePieceUpdateInterval.WaitForNextTickAsync().AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(trackerTask, updateIntervalTask, peerDeletionTask, trackerTask, rarePieceUpdateTask);
            if (ready == trackerTask)
            {
                var (id, stream) = trackerTask.Result;
                AddPeer(id, stream);
                trackerTask = trackerReceiver.ReadAsync().AsTask();
            }
            else if (ready == updateIntervalTask)
            {
                await updateIntervalTask;
                var (interesting, unChoked) = GetPeerRelations();
                await UpdateRelations(interesting, unChoked);
                updateIntervalTask = Task.Delay(_download.Config.PeerUpdateInterval);
            }
            else if (ready == peerDeletionTask)
            {
                int peerIndex = peerDeletionTask.Result;
                RemovePeer(peerIndex);
                peerDeletionTask = _peerDeleter.Reader.ReadAsync().AsTask();
            }
            else if (ready == trackerUpdateTask)
            {
                if (trackerUpdateTask is Task<TrackerResponse> responseTask)
                {
                    var response = await responseTask;
                    trackerUpdateTask = Task.Delay(response.Interval);
                    await UpdatePeers(response.Peers);
                }
                else
                {
                    trackerUpdateTask = trackerFetcher.FetchAsync(GetTrackerUpdate(null));
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
        }
    }

    private async Task Update()
    {
        var (interesting, unChoked) = GetPeerRelations();
        await UpdateRelations(interesting, unChoked);
        var recent = _download.ResetRecentTransfer();
        _transfered.FetchAdd(recent);
    }

    private TrackerUpdate GetTrackerUpdate(TrackerEvent? trackerEvent)
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
            interestingPeersLookup[interesting.Index] = true;
        }

        foreach (var unchoke in unchokedPeers)
        {
            unchokedPeersLookup[unchoke.Index] = true;
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
            int count = _peers.Where(peer => peer is not null).Select(peer => peer!.Data.OwnedPieces[i] ? 1 : 0).Sum();
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
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var peer in _peers)
        {
            peer.RelationEventWriter.Complete();
            await peer.Data.Completion.Task;
        }
        await _download.DisposeAsync();
    }
}
