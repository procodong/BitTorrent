using System.Collections.Concurrent;
using BitTorrentClient.Application.Events.Handling.PeerManagement;
using BitTorrentClient.Application.Events.Listening.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Application.Launchers.Peers.Default;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Launchers.Downloads.Default;
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
        var blockStorage = new BlockStorage(downloadState.Download.Torrent, dataStorage, haveChannel.Writer, stateChannel.Writer);
        var launcher = new PeerLauncher(downloader, removalChannel.Writer, download.Torrent.NumberOfPieces, blockStorage, _logger);
        var spawner = new PeerConnector(downloadState, _logger, removalChannel.Writer, peerAdditionChannel.Writer, Encoding.ASCII.GetBytes(downloadState.Download.ClientId));
        var peers = new PeerCollection(spawner, launcher, downloadState.Download.Config.MaxParallelPeers);
        var peerManager = new PeerManager(peers, downloadState, dataStorage);
        var relationHandler = new PeerRelationHandler();
        var eventHandler = new PeerManagerEventHandler(peerManager, relationHandler, downloadState.Download.Config.PeerUpdateInterval / downloadState.Download.Config.TransferRateResetInterval);
        var eventListener = new PeerManagerEventListener(eventHandler, removalChannel.Reader, haveChannel.Reader, stateChannel.Reader, peerAdditionChannel.Reader, tracker, downloadState.Download.Config.TransferRateResetInterval, _logger);
        _ = LaunchDownload(storage, eventListener, canceller.Token);
        return new PeerManagerHandle(peerManager, stateChannel.Writer, canceller, downloadState.Download.Torrent.OriginalInfoHashBytes, spawner, download);
    }

    private static async Task LaunchDownload(StorageStream storage, PeerManagerEventListener events, CancellationToken cancellationToken = default)
    {
        await using var _ = events;
        await using var __ = storage;
        await events.ListenAsync(cancellationToken);
    }
}
