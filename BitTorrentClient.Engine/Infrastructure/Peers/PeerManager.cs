using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Core.Presentation.UdpTracker.Models;
using BitTorrentClient.Engine.Storage.Interface;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
public sealed class PeerManager : IPeerManager, IDisposable, IAsyncDisposable
{
    private readonly PeerCollection _peers;
    private readonly DownloadState _downloadState;
    private readonly DataStorage _storage;
    private readonly IBlockAssigner _blockAssigner;
    private readonly IPieceSelectionStrategy _pieceSelectionStrategy;

    public PeerManager(PeerCollection peers, DownloadState downloadState, DataStorage storage, IBlockAssigner blockAssigner, IPieceSelectionStrategy pieceSelectionStrategy)
    {
        _peers = peers;
        _downloadState = downloadState;
        _storage = storage;
        _blockAssigner = blockAssigner;
        _pieceSelectionStrategy = pieceSelectionStrategy;
    }

    public void ResetRecentDataTransfer()
    {
        foreach (var peer in _peers)
        {
            peer.LastStatistics = peer.State.DataTransfer.Fetch();
        }
        _downloadState.DataTransfer.AtomicAdd(_downloadState.RecentDataTransfer.Fetch());
        _downloadState.ResetRecentTransfer();
    }

    public DownloadStatistics GetStatistics()
    {
        return new(_downloadState.TransferRate, _downloadState.Download.Settings.TargetDataTransferPerSecond, _peers.Count);
    }

    public IEnumerable<PeerStatistics> GetPeerStatistics()
    {
        return _peers.Select(peer =>
            new PeerStatistics(
                (peer.State.DataTransfer.Fetch() - peer.LastStatistics) / _downloadState.ElapsedSinceRecentReset,
                peer.State.RelationToMe,
                peer.State.Relation
            ));
    }

    public void UpdatePieceSelection()
    {
        _blockAssigner.SupplyPieces((buf, pieces) => _pieceSelectionStrategy.SelectPieces(pieces, _peers.Select(p => p.State.OwnedPieces), buf));
    }

    public TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent)
    {
        return new(_downloadState.Download.Data.InfoHash, _downloadState.Download.ClientId, _downloadState.DataTransfer.Fetch(), _downloadState.Download.Data.Size - _downloadState.DataTransfer.Downloaded, trackerEvent);
    }

    public IPeerCollection Peers => _peers;

    public async Task PauseAsync(PauseType type, CancellationToken cancellationToken = default)
    {
        _downloadState.ExecutionState = type == PauseType.ByUser ? DownloadExecutionState.PausedByUser : DownloadExecutionState.PausedAutomatically;
        foreach (var peer in _peers)
        {
            await peer.RelationEventWriter.WriteAsync(default, cancellationToken);
        }
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        _downloadState.ExecutionState = DownloadExecutionState.Running;
        foreach (var peer in _peers)
        {
            await peer.RelationEventWriter.WriteAsync(new(long.MaxValue, long.MaxValue), cancellationToken);
        }
        _storage.TryWritesAgain();
    }

    public async Task UpdateRelationsAsync(IEnumerable<DataTransferVector> relations, CancellationToken cancellationToken = default)
    {
        if (_downloadState.ExecutionState != DownloadExecutionState.Running) return;
        foreach (var (peer, relation) in _peers.Zip(relations))
        {
            await peer.RelationEventWriter.WriteAsync(relation, cancellationToken);
        }
    }

    public async Task NotifyPieceCompletionAsync(int piece, CancellationToken cancellationToken = default)
    {
        foreach (var peer in _peers)
        {
            await peer.HaveEventWriter.WriteAsync(piece, cancellationToken);
        }
    }

    public void Dispose()
    {
        foreach (var peer in _peers)
        {
            peer.Canceller.Cancel();
        }
        _storage.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        foreach (var peer in _peers)
        {
            peer.Canceller.Cancel();
        }
        return _storage.DisposeAsync();
    }
}
