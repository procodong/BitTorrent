using BitTorrentClient.Application.Events.EventHandling.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerManager : IPeerManager, IApplicationUpdateProvider, IAsyncDisposable
{
    private readonly PeerCollection _peers;
    private readonly DownloadState _downloadState;
    private DownloadExecutionState _state;

    public PeerManager(PeerCollection peers, DownloadState downloadState)
    {
        _peers = peers;
        _downloadState = downloadState;
    }

    public void ResetResentDataTransfer()
    {
        foreach (var peer in _peers)
        {
            peer.LastStatistics = peer.State.DataTransfer.Fetch();
        }
        _downloadState.DataTransfer.AtomicAdd(_downloadState.RecentDataTransfer.Fetch());
        _downloadState.ResetRecentTransfer();
    }

    public IPeerCollection Peers => _peers;

    public IEnumerable<PeerStatistics> Statistics => _peers.Select(peer => 
        new PeerStatistics(
            (peer.State.DataTransfer.Fetch() - peer.LastStatistics) / _downloadState.ElapsedSinceRecentReset,
            peer.State.RelationToMe,
            peer.State.Relation
        ));

    public TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent)
    {
        return new(_downloadState.Download.Torrent.OriginalInfoHashBytes, _downloadState.Download.ClientId, _downloadState.DataTransfer.Fetch(), _downloadState.Download.Torrent.TotalSize - _downloadState.DataTransfer.Downloaded, trackerEvent);
    }

    public async Task PauseAsync(PauseType type, CancellationToken cancellationToken = default)
    {
        _state = type == PauseType.ByUser ? DownloadExecutionState.PausedByUser : DownloadExecutionState.PausedAutomatically;
        foreach (var peer in _peers)
        {
            await peer.RelationEventWriter.WriteAsync(default, cancellationToken);
        }
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        _state = DownloadExecutionState.Running;
        foreach (var peer in _peers)
        {
            await peer.RelationEventWriter.WriteAsync(new(true, false), cancellationToken);
        }
    }

    public async Task UpdateRelationsAsync(IEnumerable<PeerRelation> relations, CancellationToken cancellationToken = default)
    {
        foreach (var (peer, relation) in _peers.Zip(relations))
        {
            await peer.RelationEventWriter.WriteAsync(relation, cancellationToken);
        }
    }

    public DownloadUpdate GetUpdate()
    {
        return new(_downloadState.Download.Name, _downloadState.DataTransfer.Fetch(), _downloadState.TransferRate, _downloadState.Download.Torrent.TotalSize, _state);
    }

    public async ValueTask DisposeAsync()
    {
    }
}














