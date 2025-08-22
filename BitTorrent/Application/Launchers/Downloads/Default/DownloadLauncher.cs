using BitTorrentClient.Application.Events.EventHandling.PeerManagement;
using BitTorrentClient.Application.Events.EventListening.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Downloads;
using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Networking.Trackers;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
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

    public PeerManagerHandle LaunchDownload(DownloadState download, ITrackerFetcher tracker)
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
        var spawner = new PeerSpawner(download, _logger, removalChannel.Writer, peerAdditionChannel.Writer, Encoding.ASCII.GetBytes(download.Download.ClientId));
        var peers = new PeerCollection(spawner, download.Download.Torrent.NumberOfPieces, download.Download.Config.MaxParallelPeers);
        var peerManager = new PeerManager(peers, download);
        var eventHandler = new PeerManagerEventHandler(peerManager, );
        var eventListener = new PeerManagerEventListener(removalChannel.Reader, stateChannel.Reader, peerAdditionChannel.Reader, eventHandler, tracker, download.Download.Config.PeerUpdateInterval);
        var canceller = new CancellationTokenSource();
        _ = LaunchDownload(peerManager, eventListener, canceller.Token);
        return new PeerManagerHandle(peerManager, canceller, download.Download.Torrent.OriginalInfoHashBytes, spawner);
    }

    private async Task LaunchDownload(PeerManager peerManager, PeerManagerEventListener events, CancellationToken cancellationToken = default)
    {
        await using var _ = peerManager;
        await events.ListenAsync(cancellationToken);
    }
}
