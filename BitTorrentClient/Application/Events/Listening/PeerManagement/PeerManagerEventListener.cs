using System.Threading.Channels;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Presentation.UdpTracker.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Events.Listening.PeerManagement;

public class PeerManagerEventListener : IEventListener, IDisposable, IAsyncDisposable
{
    private readonly ChannelReader<int?> _peerRemovalReader;
    private readonly ChannelReader<int> _pieceCompletionReader;
    private readonly ChannelReader<PeerWireStream> _peerReader;
    private readonly ChannelReader<DownloadExecutionState> _stateReader;
    private readonly ITrackerFetcher _trackerFetcher;
    private readonly int _updateInterval;
    private readonly IPeerManagerEventHandler _handler;
    private readonly ILogger _logger;

    public PeerManagerEventListener(IPeerManagerEventHandler handler, ChannelReader<int?> peerRemovalReader, ChannelReader<int> pieceCompletionReader, ChannelReader<DownloadExecutionState> stateReader, ChannelReader<PeerWireStream> peerReader, ITrackerFetcher trackerFetcher, int updateInterval, ILogger logger)
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
        Task<PeerWireStream> peerAdditionTask = _peerReader.ReadAsync(cancellationToken).AsTask();
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateInterval));
        Task updateIntervalTask = updateInterval.WaitForNextTickAsync(cancellationToken).AsTask();
        Task<int?> peerRemovalTask = _peerRemovalReader.ReadAsync(cancellationToken).AsTask();
        Task<int> pieceCompletionTask = _pieceCompletionReader.ReadAsync(cancellationToken).AsTask();
        Task<DownloadExecutionState> fileExceptionTask = _stateReader.ReadAsync(cancellationToken).AsTask();
        Task trackerUpdateTask = _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.Started), cancellationToken);
        while (true)
        {
            var ready = await Task.WhenAny(peerAdditionTask, updateIntervalTask, peerRemovalTask, trackerUpdateTask, pieceCompletionTask);
            if (ready == peerAdditionTask)
            {
                try
                {
                    var stream = await peerAdditionTask;
                    await _handler.OnPeerCreationAsync(stream, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                {
                    await HandleError(ex, cancellationToken);
                }
                finally
                {
                    peerAdditionTask = _peerReader.ReadAsync(cancellationToken).AsTask();
                }
            }
            else if (ready == updateIntervalTask)
            {
                try
                {
                    await updateIntervalTask;
                    await _handler.OnTickAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                {
                    await HandleError(ex, cancellationToken);
                }
                finally
                {
                    updateIntervalTask = updateInterval.WaitForNextTickAsync(cancellationToken).AsTask();
                }
            }
            else if (ready == peerRemovalTask)
            {
                try
                {
                    int? peer = await peerRemovalTask;
                    await _handler.OnPeerRemovalAsync(peer, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                {
                    await HandleError(ex, cancellationToken);
                }
                finally
                {
                    peerRemovalTask = _peerRemovalReader.ReadAsync(cancellationToken).AsTask();
                }
            }
            else if (ready == trackerUpdateTask)
            {
                if (trackerUpdateTask is Task<TrackerResponse> responseTask)
                {
                    try
                    {
                        var response = await responseTask;
                        await _handler.OnTrackerUpdate(response, cancellationToken);
                        trackerUpdateTask = Task.Delay(response.Interval * 1000, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                    {
                        trackerUpdateTask = Task.Delay(5000, cancellationToken);
                        await HandleError(ex, cancellationToken);
                    }
                }
                else
                {
                    trackerUpdateTask = _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.None), cancellationToken);
                }
            }
            else if (ready == fileExceptionTask)
            {
                try
                {
                    var state = await fileExceptionTask;
                    await _handler.OnStateChange(state, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                {
                    await HandleError(ex, cancellationToken);
                }
                finally
                {
                    fileExceptionTask = _stateReader.ReadAsync(cancellationToken).AsTask();
                }
            }
            else if (ready == pieceCompletionTask)
            {
                try
                {
                    var piece = await pieceCompletionTask;
                    await _handler.OnPieceCompletionAsync(piece, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                {
                    await HandleError(ex, cancellationToken);
                }
                finally
                {
                    pieceCompletionTask = _pieceCompletionReader.ReadAsync(cancellationToken).AsTask();
                }
            }
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
        if (_trackerFetcher is IDisposable disposable)
        {
            disposable.Dispose();
        }
        if (_handler is IDisposable handlerDisposable)
        {
            handlerDisposable.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_trackerFetcher is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        if (_handler is IAsyncDisposable handlerDisposable)
        {
            await handlerDisposable.DisposeAsync();
        }
    }
}
