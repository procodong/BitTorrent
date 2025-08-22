using BitTorrentClient.Application.Events.Listening.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Peers.Interface;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling;
internal class PeerManagerEventHandler : IPeerManagerEventHandler
{
    private readonly IPeerManager _peerManager;
    private readonly IPeerRelationHandler _relationHandler;
    private readonly int _relationUpdateInterval;
    private int _tick;

    public PeerManagerEventHandler(IPeerManager peerManager, IPeerRelationHandler relationHandler, int relationUpdateInterval)
    {
        _peerManager = peerManager;
        _relationHandler = relationHandler;
        _relationUpdateInterval = relationUpdateInterval;
    }

    public TrackerUpdate GetTrackerUpdate(TrackerEvent trackerEvent)
    {
        return _peerManager.GetTrackerUpdate(trackerEvent);
    }

    public Task OnPeerCreationAsync(PeerWireStream stream, CancellationToken cancellationToken = default)
    {
        _peerManager.Peers.Add(stream);
        return Task.CompletedTask;
    }

    public Task OnPeerRemovalAsync(int? peer, CancellationToken cancellationToken = default)
    {
        _peerManager.Peers.Remove(peer);
        return Task.CompletedTask;
    }

    public async Task OnPieceCompletionAsync(int piece, CancellationToken cancellationToken = default)
    {
        await _peerManager.NotifyPieceCompletion(piece, cancellationToken);
    }

    public async Task OnStateChange(DownloadExecutionState change, CancellationToken cancellationToken = default)
    {
        if (change == DownloadExecutionState.Running)
        {
            await _peerManager.ResumeAsync(cancellationToken);
        }
        else
        {
            await _peerManager.PauseAsync((PauseType)change, cancellationToken);
        }
    }

    public async Task OnTickAsync(CancellationToken cancellationToken = default)
    {
        if (_tick % _relationUpdateInterval == 0)
        {
            var stats = _peerManager.Statistics;
            var relations = _peerManager.GetPeerStatistics().Select(peer => _relationHandler.GetRelation(peer, stats));
            await _peerManager.UpdateRelationsAsync(relations, cancellationToken);
        }
        _peerManager.ResetResentDataTransfer();
        unchecked
        {
            _tick++;
        }
    }

    public Task OnTrackerUpdate(TrackerResponse response, CancellationToken cancellationToken = default)
    {
        _peerManager.Peers.Feed(response.Peers);
        return Task.CompletedTask;
    }
}
