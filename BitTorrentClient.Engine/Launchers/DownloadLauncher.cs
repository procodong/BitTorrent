using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling;
using BitTorrentClient.Engine.Events.Listening;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Engine.Storage.Distribution;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Launchers;
public sealed class DownloadLauncher : IDownloadLauncher
{
    private readonly ILogger _logger;

    public DownloadLauncher(ILogger logger)
    {
        _logger = logger;
    }

    public PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker)
    {
        var options = new BoundedChannelOptions(8)
        {
            SingleWriter = false
        };
        var peerAdditionChannel = Channel.CreateBounded<PeerWireStream>(options);
        var peerRemovalChannel = Channel.CreateBounded<ReadOnlyMemory<byte>?>(options);
        var stateChannel = Channel.CreateBounded<DownloadExecutionState>(options);
        var haveChannel = Channel.CreateBounded<int>(options);
        
        var downloadState = new DownloadState(download);
        var canceller = new CancellationTokenSource();
        var blockAssigner = new BlockAssigner(download.Data, download.Config.PieceSegmentSize);
        var dataStorage = new DataStorage(storage, stateChannel.Writer, canceller.Token);
        var blockStorage = new BlockStorage(downloadState.Download.Data, dataStorage, haveChannel.Writer);
        var launcher = new PeerLauncher(new(blockAssigner), downloadState, peerRemovalChannel.Writer, download.Data.PieceCount, TimeSpan.FromMilliseconds(download.Config.KeepAliveInterval), blockStorage, _logger);
        var spawner = new PeerConnector(downloadState.Download, downloadState.DownloadedPieces, peerRemovalChannel.Writer, peerAdditionChannel.Writer, _logger);
        var peers = new PeerCollection(spawner, launcher, downloadState.Download.Config.MaxParallelPeers);
        var peerManager = new PeerManager(peers, downloadState, dataStorage);
        var relationHandler = new PeerRelationHandler();
        var eventHandler = new PeerManagerEventHandler(peerManager, relationHandler, downloadState.Download.Config.PeerUpdateInterval / downloadState.Download.Config.TransferRateResetInterval);
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(downloadState.Download.Config.TransferRateResetInterval));
        var eventListener = new PeerManagerEventListener(eventHandler, peerRemovalChannel.Reader, haveChannel.Reader, stateChannel.Reader, peerAdditionChannel.Reader, tracker, updateInterval, _logger);
        var task = LaunchDownload(eventListener, canceller.Token);
        return new PeerManagerHandle(downloadState, stateChannel.Writer, canceller, spawner, task);
    }

    private static async Task LaunchDownload(PeerManagerEventListener events, CancellationToken cancellationToken = default)
    {
        await using (events)
        {
            await events.ListenAsync(cancellationToken);
        }
    }
}
