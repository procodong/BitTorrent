using BitTorrentClient.Application.EventHandling.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerManager : IPeerManager
{
    private readonly PeerCollection _peers;
    private readonly DownloadState _download;
    private DownloadExecutionState _state;

    public PeerManager(PeerCollection peers, DownloadState download)
    {
        _peers = peers;
        _download = download;
    }

    public void ResetResentDataTransfer()
    {
        throw new NotImplementedException();
    }

    public IPeerCollection Peers => _peers;

    public IEnumerable<PeerStatistics> Statistics => _peers.Select(h => new PeerStatistics());

    public TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent)
    {
        return new(_download.Torrent.OriginalInfoHashBytes, _download.ClientId, _download.DataTransfer.Fetch(), _download.Torrent.TotalSize - _download.DataTransfer.Downloaded, trackerEvent);
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
}
