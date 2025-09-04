using System.Text;
using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling;
using BitTorrentClient.Engine.Events.Listening;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers.Interface;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Launchers;
public class DownloadLauncher : IDownloadLauncher
{
    private readonly ILogger _logger;

    public DownloadLauncher(ILogger logger)
    {
        _logger = logger;
    }

    public PeerManagerHandle LaunchDownload(Download download, StorageStream storage, ITrackerFetcher tracker)
    {
        var peerAdditionChannel = Channel.CreateBounded<PeerWireStream>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var removalChannel = Channel.CreateBounded<int?>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var stateChannel = Channel.CreateBounded<DownloadExecutionState>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var haveChannel = Channel.CreateBounded<int>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var downloadState = new DownloadState(download);
        var canceller = new CancellationTokenSource();
        var downloader = new Downloader(downloadState);
        var dataStorage = new DataStorage(storage, stateChannel.Writer, canceller.Token);
        var blockStorage = new BlockStorage(downloadState.Download.Data, dataStorage, haveChannel.Writer);
        var launcher = new PeerLauncher(downloader, removalChannel.Writer, download.Data.PieceCount, TimeSpan.FromMilliseconds(download.Config.KeepAliveInterval), blockStorage, _logger);
        var spawner = new PeerConnector(downloadState, _logger, removalChannel.Writer, peerAdditionChannel.Writer, Encoding.ASCII.GetBytes(downloadState.Download.ClientId));
        var peers = new PeerCollection(spawner, launcher, downloadState.Download.Config.MaxParallelPeers);
        var peerManager = new PeerManager(peers, downloadState, dataStorage);
        var relationHandler = new PeerRelationHandler();
        var eventHandler = new PeerManagerEventHandler(peerManager, relationHandler, downloadState.Download.Config.PeerUpdateInterval / downloadState.Download.Config.TransferRateResetInterval);
        var updateInterval = new PeriodicTimer(TimeSpan.FromMilliseconds(downloadState.Download.Config.TransferRateResetInterval));
        var eventListener = new PeerManagerEventListener(eventHandler, removalChannel.Reader, haveChannel.Reader, stateChannel.Reader, peerAdditionChannel.Reader, tracker, updateInterval, _logger);
        var task = LaunchDownload(eventListener, canceller.Token);
        return new PeerManagerHandle(downloadState, stateChannel.Writer, canceller, spawner, task);
    }

    private static async Task LaunchDownload(PeerManagerEventListener events, CancellationToken cancellationToken = default)
    {
        await using var _ = events;
        await events.ListenAsync(cancellationToken);
    }
}
