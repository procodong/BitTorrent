using BitTorrentClient.Models.Trackers;
using System.Threading.Channels;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.Trackers;

namespace BitTorrentClient.Application.EventListening.PeerManagement;

public class PeerManagerEventListener
{
    private readonly ChannelReader<int?> _peerRemovalReader;
    private readonly ChannelReader<RespondedHandshakeHandler> _peerReader;
    private readonly ChannelReader<DownloadExecutionState> _stateReader;
    private readonly ITrackerFetcher _trackerFetcher;
    private readonly int _updateInterval;
    private readonly IPeerManagerEventHandler _handler;

    public PeerManagerEventListener(ChannelReader<int?> peerRemovalReader, ChannelReader<DownloadExecutionState> stateReader, ChannelReader<RespondedHandshakeHandler> peerReader, IPeerManagerEventHandler handler, ITrackerFetcher trackerFetcher, int updateInterval)
    {
        _peerRemovalReader = peerRemovalReader;
        _stateReader = stateReader;
        _peerReader = peerReader;
        _trackerFetcher = trackerFetcher;
        _updateInterval = updateInterval;
        _handler = handler;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<RespondedHandshakeHandler> peerAdditionTask = _peerReader.ReadAsync(cancellationToken).AsTask();
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateInterval));
        Task updateIntervalTask = updateInterval.WaitForNextTickAsync(cancellationToken).AsTask();
        Task<int?> peerRemovalTask = _peerRemovalReader.ReadAsync(cancellationToken).AsTask();
        Task<DownloadExecutionState> fileExceptionTask = _stateReader.ReadAsync(cancellationToken).AsTask();
        Task trackerUpdateTask = _trackerFetcher.FetchAsync(_handler.GetTrackerUpdate(TrackerEvent.Started), cancellationToken);
        while (true)
        {
            var ready = await Task.WhenAny(peerAdditionTask, updateIntervalTask, peerRemovalTask, trackerUpdateTask);
            if (ready == peerAdditionTask)
            {
                try
                {
                    var stream = await peerAdditionTask;
                    await _handler.OnPeerCreationAsync(stream, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not ChannelClosedException)
                {
                    await HandleError(cancellationToken);
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
                    await HandleError(cancellationToken);
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
                    await HandleError(cancellationToken);
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
                        await HandleError(cancellationToken);
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
                    await HandleError(cancellationToken);
                }
                finally
                {
                    fileExceptionTask = _stateReader.ReadAsync(cancellationToken).AsTask();
                }
            }
        }
    }

    private async Task HandleError(CancellationToken cancellationToken)
    {
        await _handler.OnStateChange(DownloadExecutionState.PausedAutomatically, cancellationToken);
        DownloadExecutionState state;
        do
        {
            state = await _stateReader.ReadAsync(cancellationToken);
        } 
        while (state != DownloadExecutionState.Running);
    }
}
