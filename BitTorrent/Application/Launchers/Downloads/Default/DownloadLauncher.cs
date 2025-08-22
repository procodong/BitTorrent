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

    public PeerManagerHandle LaunchDownload(DownloadState downloadState, DownloadStorage storage, ITrackerFetcher tracker)
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
        var downloader = new Downloader(downloadState);
        var blockStorage = new BlockStorage(haveChannel.Writer, downloadState.Download.Torrent, storage);
        var launcher = new PeerLauncher(downloader, blockStorage, stateChannel.Writer);
        var spawner = new PeerSpawner(downloadState, launcher, _logger, removalChannel.Writer, peerAdditionChannel.Writer, Encoding.ASCII.GetBytes(downloadState.Download.ClientId));
        var peers = new PeerCollection(spawner, downloadState.Download.Config.MaxParallelPeers);
        var peerManager = new PeerManager(peers, downloadState);
        var eventHandler = new PeerManagerEventHandler(peerManager, _, downloadState.Download.Config.PeerUpdateInterval / downloadState.Download.Config.TransferRateResetInterval);
        var eventListener = new PeerManagerEventListener(eventHandler, removalChannel.Reader, haveChannel.Reader, stateChannel.Reader, peerAdditionChannel.Reader, tracker, downloadState.Download.Config.TransferRateResetInterval);
        var canceller = new CancellationTokenSource();
        _ = LaunchDownload(storage, eventListener, canceller.Token);
        return new PeerManagerHandle(peerManager, canceller, downloadState.Download.Torrent.OriginalInfoHashBytes, spawner);
    }

    private static async Task LaunchDownload(DownloadStorage storage, PeerManagerEventListener events, CancellationToken cancellationToken = default)
    {
        await using var _ = events;
        await using var __ = storage;
        await events.ListenAsync(cancellationToken);
    }
}
