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

namespace BitTorrent.Torrents.PeerManaging;
public class PeerManager(int id, string peerId, string trackerUrl, Download download) : IDisposable, IAsyncDisposable
{
    private readonly SlotMap<PeerConnector> _peers = [];
    private readonly HashSet<string> _peerIds = [];
    private readonly string _peerId = peerId;
    private readonly string _trackerUrl = trackerUrl;
    private readonly int _id = id;
    private readonly Download _download = download;
    private readonly Stopwatch _updateWatch = new();
    private readonly Channel<int> _peerDeleter = Channel.CreateBounded<int>(new BoundedChannelOptions(8)
    {
        SingleWriter = false
    });

    public void AddPeer(string peerId, PeerWireStream stream)
    {
        var eventChannel = Channel.CreateBounded<PeerRelation>(8);
        var haveChannel = Channel.CreateUnbounded<int>();
        var stats = new SharedPeerData(new(_download.Torrent.NumberOfPieces));
        _download.AddPeer(haveChannel.Writer);
        var peer = new PeerConnector(stats, eventChannel.Writer, new(), new(), peerId);
        _peerIds.Add(peerId);
        int index = _peers.Add(peer);
        Task.Run(async () =>
        {
            try
            {
                var handshake = new HandShake("BitTorrent protocol", _download.Torrent.OriginalInfoHashBytes, _peerId);
                await stream.InitializeConnectionAsync(_download.DownloadedPiecesCount != 0 ? _download.DownloadedPieces : null, handshake);
                await using var peer = new Peer(stream, haveChannel.Reader, eventChannel.Reader, _download, stats);
                await peer.ListenAsync();
            }
            finally
            {
                await _peerDeleter.Writer.WriteAsync(index);
            }
        });
    }

    private void RemovePeer(int index)
    {
        _download.RemovePeer(index);
        var peer = _peers[index]!;
        peer.RelationEventWriter.Complete();
        _peerIds.Remove(peer.PeerId);
        _peers.Remove(index);
    }

    private async Task UpdatePeers(List<PeerAddress> peers)
    {
        foreach (PeerAddress peer in peers)
        {
            if (_peerIds.Contains(peer.Id)) continue;
            var connection = new TcpClient
            {
                ReceiveTimeout = _download.Config.ReceivedTimeout
            };
            await connection.ConnectAsync(peer.Ip, peer.Port);
            var stream = new NetworkStream(connection.Client, true);
            var peerStream = new PeerWireStream(stream);
            var handshake = await peerStream.ReadHandShakeAsync();
            if (!handshake.InfoHash.SequenceEqual(_download.Torrent.OriginalInfoHashBytes))
            {
                throw new BadPeerException(PeerErrorReason.InvalidInfoHash);
            }
            AddPeer(handshake.PeerId, peerStream);
        }
        var peerIds = peers.Select(peer => peer.Id).ToHashSet();
        foreach (string id in _peerIds)
        {
            if (!_peerIds.Contains(id))
            {
                var peer = _peers.Indexed().Where(p => p.Value.PeerId == id).First();
                RemovePeer(peer.Index);
            }
        }
    }

    public async Task ListenAsync(ChannelReader<TrackerHandlerEvent> trackerReceiver, ChannelWriter<TrackerUpdate> trackerUpdater)
    {
        Task<TrackerHandlerEvent> trackerTask = trackerReceiver.ReadAsync().AsTask();
        Task updateIntervalTask = Task.Delay(_download.Config.PeerUpdateInterval);
        Task<int> peerDeletionTask = _peerDeleter.Reader.ReadAsync().AsTask();
        Task trackerIntervalTask = Task.CompletedTask;
        var rarePieceUpdateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_download.Config.RarePiecesUpdateInterval));
        Task rarePieceUpdateTask = rarePieceUpdateInterval.WaitForNextTickAsync().AsTask();
        while (true)
        {
            var ready = await Task.WhenAny(trackerTask, updateIntervalTask, peerDeletionTask, trackerIntervalTask, rarePieceUpdateTask);
            if (ready == trackerTask)
            {
                var result = trackerTask.Result;
                switch (result.Type)
                {
                    case TrackerHandlerEventType.Stream:
                        var (id, client) = result.Stream;
                        AddPeer(id, client);
                        break;
                    case TrackerHandlerEventType.TrackerResponse:
                        TrackerResponse response = result.Response;
                        trackerIntervalTask = Task.Delay(response.Interval);
                        await UpdatePeers(response.Peers);
                        break;
                }
                trackerTask = trackerReceiver.ReadAsync().AsTask();
            }
            else if (ready == updateIntervalTask)
            {
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
            else if (ready == trackerIntervalTask)
            {
                TrackerEvent? trackerEvent = trackerIntervalTask == Task.CompletedTask ? TrackerEvent.Started : null;
                await UpdateTracker(trackerEvent, trackerUpdater);
                trackerIntervalTask = Task.Delay(Timeout.Infinite);
            }
            else if (ready == rarePieceUpdateTask)
            {
                lock (_download)
                {
                    _download.RarestPieces = [..FindRaresPieces()];
                }
                rarePieceUpdateTask = rarePieceUpdateInterval.WaitForNextTickAsync().AsTask();
            }
        }
    }

    private async Task UpdateTracker(TrackerEvent? trackerEvent, ChannelWriter<TrackerUpdate> trackerUpdater)
    {
        var transfer = _download.DataTransfered;
        var update = new TrackerUpdate(_download.Torrent.OriginalInfoHashBytes, _peerId, transfer, _download.Torrent.TotalSize - transfer.Downloaded, trackerEvent, _trackerUrl, _id);
        await trackerUpdater.WriteAsync(update);
    }

    private (BitArray Interesting, BitArray UnChoked) GetPeerRelations()
    {
        var interestingPeers = new SumPriorityStack<int>(_download.Config.TargetDownload);
        var unchokedPeers = new SumPriorityStack<int>(_download.FinishedDownloading ? _download.Config.TargetUploadSeeding : _download.Config.TargetUpload);

        foreach (var (index, peer) in _peers.Indexed())
        {
            if (peer is null) continue;
            bool interested = peer.Data.Relation.Interested;
            bool choked = peer.Data.Relation.Choked;
            var stats = new DataTransferVector(
                choked ? peer.LastUnchokedStats.Downloaded : Interlocked.Read(ref peer.Data.Stats.Downloaded), 
                !interested ? peer.LastUnchokedStats.Uploaded : Interlocked.Read(ref peer.Data.Stats.Uploaded)
                );
            var statChange = (stats - peer.LastStatistics) / _updateWatch.Elapsed.TotalSeconds;
            unchokedPeers.Include(index, (int)statChange.Uploaded, (int)statChange.Uploaded);
            interestingPeers.Include(index, (int)statChange.Downloaded, (int)statChange.Downloaded);
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
            if (peer is null) continue;
            bool interesting = interestingPeers[index];
            bool unChoked = unchokedPeers[index];
            if (!interesting)
            {
                peer.LastUnchokedStats.Uploaded = peer.LastStatistics.Downloaded;
            }
            if (!unChoked)
            {
                peer.LastUnchokedStats.Downloaded = peer.LastStatistics.Downloaded;
            }
            await peer.RelationEventWriter.WriteAsync(new(interesting, !unChoked));
        }
    }

    private int[] FindRaresPieces()
    {
        int rareCount = int.Min(_download.Torrent.NumberOfPieces / 10, _download.Config.MaxRarePieceCount);
        var comparer = Comparer<(int Index, int Count)>.Create((v1, v2) => v1.Count - v2.Count);
        var rarestPieceStack = new PriorityStack<(int Index, int Count)>(rareCount, comparer);
        foreach (var piece in PieceCounts())
        {
            rarestPieceStack.Include(piece);
        }
        int[] rarePieces = new int[rareCount];
        foreach (var (index, piece) in rarestPieceStack)
        {
            rarePieces[index] = piece;
        }
        return rarePieces;
    }

    private IEnumerable<(int Index, int Count)> PieceCounts()
    {
        for (int i = 0; i < _download.Torrent.NumberOfPieces; i++)
        {
            int count = _peers.Where(peer => peer is not null).Select(peer => peer!.Data.OwnedPieces[i] ? 1 : 0).Sum();
            yield return (i,count);
        }
    }

    public void Dispose()
    {
        _download.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _download.DisposeAsync();
    }
}
