using BitTorrentClient.Application.Events.Listening.PeerManagement;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Application.Events.Handling.PeerManagement;
public class PeerManagerEventHandler : IPeerManagerEventHandler
{
    private readonly IPeerManager _peerManager;
    private readonly IPeerRelationHandler _relationHandler;

    public PeerManagerEventHandler(IPeerManager peerManager, IPeerRelationHandler relationHandler)
    {
        _peerManager = peerManager;
        _relationHandler = relationHandler;
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
        var relations = _relationHandler.GetRelations(_peerManager.Statistics);
        await _peerManager.UpdateRelationsAsync(relations, cancellationToken);
    }

    public Task OnTrackerUpdate(TrackerResponse response, CancellationToken cancellationToken = default)
    {
        _peerManager.Peers.Feed(response.Peers);
        return Task.CompletedTask;
    }
}
