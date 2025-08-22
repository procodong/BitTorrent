using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Events.Handling;
public class PeerManagerEventHandler : IPeerManagerEventHandler, IDisposable, IAsyncDisposable
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
            var stats = _peerManager.GetStatistics();
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
        _peerManager.Peers.Feed(response.Peers.ToList());
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_peerManager is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_peerManager is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_peerManager is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
