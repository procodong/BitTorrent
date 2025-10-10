using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Events.Listening;

public sealed class PeerManagerEventListener : IEventListener, IDisposable, IAsyncDisposable
{
    private readonly ChannelReader<ReadOnlyMemory<byte>?> _peerRemovalReader;
    private readonly ChannelReader<int> _pieceCompletionReader;
    private readonly ChannelReader<PeerWireStream> _peerReader;
    private readonly ChannelReader<DownloadExecutionState> _stateReader;
    private readonly ITrackerFetcher _trackerFetcher;
    private readonly PeriodicTimer _updateInterval;
    private readonly IPeerManagerEventHandler _handler;
    private readonly ILogger _logger;

    public PeerManagerEventListener(IPeerManagerEventHandler handler, ChannelReader<ReadOnlyMemory<byte>?> peerRemovalReader, ChannelReader<int> pieceCompletionReader, ChannelReader<DownloadExecutionState> stateReader, ChannelReader<PeerWireStream> peerReader, ITrackerFetcher trackerFetcher, PeriodicTimer updateInterval, ILogger logger)
    {
        _pieceCompletionReader = pieceCompletionReader;
        _peerRemovalReader = peerRemovalReader;
        _stateReader = stateReader;
        _peerReader = peerReader;
        _trackerFetcher = trackerFetcher;
        _updateInterval = updateInterval;
        _handler = handler;
        _logger = logger;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        var taskListener = new TaskListener<EventType>(cancellationToken);
        taskListener.AddTask(EventType.PeerAddition, () => _peerReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.UpdateInterval, () => _updateInterval.WaitForNextTickAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.PeerRemoval, () => _peerRemovalReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.PieceCompletion, () => _pieceCompletionReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.StateUpdate, () => _stateReader.ReadAsync(cancellationToken).AsTask());
        taskListener.AddTask(EventType.TrackerUpdate, _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.Started), cancellationToken));
        while (true)
        {
            var (eventType, readyTask) = await taskListener.WaitAsync();
            try
            {
                await HandleEventAsync(eventType, readyTask, taskListener, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
            {
                await HandleError(ex, cancellationToken);
            }
        }
    }

    private async Task HandleEventAsync(EventType eventType, Task readyTask, TaskListener<EventType> taskListener, CancellationToken cancellationToken)
    {
        
        switch (eventType)
        {
            case EventType.PeerAddition:
                var stream = await (Task<PeerWireStream>)readyTask;
                await _handler.OnPeerCreationAsync(stream, cancellationToken);
                break;
            case EventType.UpdateInterval:
                await _handler.OnTickAsync(cancellationToken);
                break;
            case EventType.PeerRemoval:
                var peer = await (Task<ReadOnlyMemory<byte>>)readyTask;
                await _handler.OnPeerRemovalAsync(peer, cancellationToken);
                break;
            case EventType.TrackerUpdate:
                var response = await (Task<TrackerResponse>)readyTask;
                await _handler.OnTrackerUpdate(response, cancellationToken);
                taskListener.AddTask(EventType.TrackerInterval, Task.Delay(response.Interval * 1000, cancellationToken));
                break;
            case EventType.TrackerInterval:
                taskListener.AddTask(EventType.TrackerUpdate, _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.None), cancellationToken));
                break;
            case EventType.StateUpdate:
                var state = await (Task<DownloadExecutionState>)readyTask;
                await _handler.OnStateChange(state, cancellationToken);
                break;
            case EventType.PieceCompletion:
                var piece = await (Task<int>)readyTask;
                await _handler.OnPieceCompletionAsync(piece, cancellationToken);
                break;
        }
    }

    private async Task HandleError(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Peer manager {}", exception);
        await _handler.OnStateChange(DownloadExecutionState.PausedAutomatically, cancellationToken);
        DownloadExecutionState state;
        do
        {
            state = await _stateReader.ReadAsync(cancellationToken);
        }
        while (state != DownloadExecutionState.Running);
    }

    public void Dispose()
    {
        _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.Stopped), CancellationToken.None).GetAwaiter().GetResult();
        _trackerFetcher.Dispose();
        if (_handler is IDisposable handlerDisposable)
        {
            handlerDisposable.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.Stopped), CancellationToken.None);
        _trackerFetcher.Dispose();
        switch (_handler)
        {
            case IAsyncDisposable handlerAsyncDisposable:
                await handlerAsyncDisposable.DisposeAsync();
                break;
            case IDisposable handlerDisposable:
                handlerDisposable.Dispose();
                break;
        }
    }

    enum EventType {
        PeerAddition,
        UpdateInterval,
        PeerRemoval,
        TrackerUpdate,
        TrackerInterval,
        StateUpdate,
        PieceCompletion
    }
}