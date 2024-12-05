using BitTorrent.Models.Peers;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;

public delegate void NewPeer(IdentifiedPeerWireStream stream);
public delegate Task Update();
public delegate void PeerRemoval(int peerIndex);
public delegate void TrackerResponse(Models.Trackers.TrackerResponse respose);
public delegate TrackerUpdate TrackerInterval();
public delegate void Error(Exception exception);

public class PeerManagerEventHandler
{
    private readonly ChannelReader<int> _peerRemovalReader;
    private readonly ChannelReader<IdentifiedPeerWireStream> _peerReader;
    private readonly ITrackerFetcher _trackerFetcher;
    private readonly int _updateInterval;
    public NewPeer PeerAddition;
    public Update Update;
    public PeerRemoval PeerRemoval;
    public TrackerResponse TrackerResponse;
    private readonly Func<TrackerEvent, TrackerUpdate> _trackerUpdateProvider;
    public Error Error;

    public PeerManagerEventHandler(ChannelReader<int> peerRemovalReader, ChannelReader<IdentifiedPeerWireStream> peerReader, ITrackerFetcher trackerFetcher, int updateInterval, Func<TrackerEvent, TrackerUpdate> trackerUpdates)
    {
        _peerRemovalReader = peerRemovalReader;
        _peerReader = peerReader;
        _trackerFetcher = trackerFetcher;
        _updateInterval = updateInterval;
        PeerAddition = (_) => { };
        Update = () => Task.CompletedTask;
        PeerRemoval = (_) => { };
        TrackerResponse = (_) => { };
        Error = (_) => { };
        _trackerUpdateProvider = trackerUpdates;
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task<IdentifiedPeerWireStream> peerAdditionTask = _peerReader.ReadAsync(cancellationToken).AsTask();
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(_updateInterval));
        Task updateIntervalTask = updateInterval.WaitForNextTickAsync(cancellationToken).AsTask();
        Task<int> peerRemovalTask = _peerRemovalReader.ReadAsync(cancellationToken).AsTask();
        Task trackerUpdateTask = _trackerFetcher.FetchAsync(_trackerUpdateProvider(TrackerEvent.Started), cancellationToken);
        while (true)
        {
            var ready = await Task.WhenAny(peerAdditionTask, updateIntervalTask, peerRemovalTask, trackerUpdateTask);
            if (ready == peerAdditionTask)
            {
                try
                {
                    var stream = await peerAdditionTask;
                    PeerAddition(stream);
                }
                catch (Exception ex)
                {
                    Error(ex);
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
                    await Update();
                }
                catch (Exception ex)
                {
                    Error(ex);
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
                    int peer = await peerRemovalTask;
                    PeerRemoval(peer);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
                finally
                {
                    peerRemovalTask = _peerRemovalReader.ReadAsync(cancellationToken).AsTask();
                }
            }
            else if (ready == trackerUpdateTask)
            {
                if (trackerUpdateTask is Task<Models.Trackers.TrackerResponse> responseTask)
                {
                    try
                    {
                        var response = await responseTask;
                        TrackerResponse(response);
                        trackerUpdateTask = Task.Delay(response.Interval * 1000, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        trackerUpdateTask = Task.Delay(5000, cancellationToken);
                        Error(ex);
                    }
                }
                else
                {
                    trackerUpdateTask = _trackerFetcher.FetchAsync(_trackerUpdateProvider!(TrackerEvent.None), cancellationToken);
                }
            }
        }
    }
}
