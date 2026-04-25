using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling;
using BitTorrentClient.Engine.Events.Listening;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Engine.Storage.Distribution;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;
using BitTorrentClient.Core.Transport.Trackers.Interface;
using BitTorrentClient.Engine.Models.Config;
using BitTorrentClient.Engine.Storage.Interface;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Launchers;
public sealed class DownloadLauncher : IDownloadLauncher
{
    private readonly ILogger _logger;

    public DownloadLauncher(ILogger logger)
    {
        _logger = logger;
    }

    public PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker, NetworkingConfig config, IPieceSelectionStrategy pieceSelectionStrategy)
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
        var piecesCursor = new PiecesCursor(config.PiecesBufferSize);
        var blockAssigner = new BlockAssigner(download.Data, config.PieceSegmentSize, piecesCursor);
        var synchronizedBlockAssigner = new SynchronizedBlockAssigner(blockAssigner);
        var dataStorage = new DataStorage(storage, stateChannel.Writer, canceller.Token);
        var blockStorage = new BlockStorage(downloadState.Download.Data, dataStorage, haveChannel.Writer);
        var launcher = new PeerLauncher(new(blockAssigner), downloadState, peerRemovalChannel.Writer, download.Data.PieceCount, config.RequestQueueSize, config.RequestSize, config.KeepAliveInterval, blockStorage, _logger);
        var spawner = new PeerConnector(downloadState.Download, downloadState.DownloadedPieces, peerRemovalChannel.Writer, peerAdditionChannel.Writer, _logger);
        var peers = new PeerCollection(spawner, launcher, download.Settings.MaxParallelPeers);
        var peerManager = new PeerManager(peers, downloadState, dataStorage, synchronizedBlockAssigner, pieceSelectionStrategy);
        var relationHandler = new PeerRelationHandler();
        var eventHandler = new PeerManagerEventHandler(peerManager, relationHandler, config.TickInterval, config.PeerUpdateTickInterval, config.TransferRateResetTickInterval);
        var updateInterval = new PeriodicTimer(config.TickInterval);
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
