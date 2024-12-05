using BencodeNET.Torrents;
using BitTorrent.Files.DownloadFiles;
using BitTorrent.Models.Application;
using BitTorrent.Models.Peers;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Managing;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Trackers;
using BitTorrent.Torrents.Trackers.Errors;
using BitTorrent.Utils;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Downloads;
public class DownloadCollection : IAsyncDisposable, IDisposable
{
    private readonly List<PeerManagerConnector> _downloads = [];
    private readonly PeerIdGenerator _peerIdGenerator = new();
    private readonly Config _config;
    private readonly ChannelWriter<PeerReceivingSubscribe> _peerReceivingSubscriber;
    private readonly ILogger _logger;
    private readonly ITrackerFinder _trackerFinder;
    public bool HasUpdates => _downloads.Count != 0;

    public DownloadCollection(ChannelWriter<PeerReceivingSubscribe> peerReceivingSubscriber, Config config, ILogger logger, ITrackerFinder trackerFinder)
    {
        _config = config;
        _logger = logger;
        _peerReceivingSubscriber = peerReceivingSubscriber;
        _trackerFinder = trackerFinder;
    }

    public async Task StartDownload(Torrent torrent, DownloadSaveManager files)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var request = new TrackerUpdate(torrent.OriginalInfoHashBytes, peerId, new(), torrent.TotalSize, TrackerEvent.Started);
        var fetcher = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(torrent, files, _config);
        var peerAdditionChannel = Channel.CreateBounded<IdentifiedPeerWireStream>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var removalChannel = Channel.CreateBounded<int>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var spawner = new PeerSpawner(download, _logger, removalChannel.Writer, peerAdditionChannel.Writer, peerId);
        var peers = new PeerCollection(spawner, _logger, torrent.NumberOfPieces);
        var peerManager = new PeerManager(peerId, download, peers, _logger, fetcher);
        var cancellationTokenSource = new CancellationTokenSource();
        _downloads.Add(new(peerManager, cancellationTokenSource, torrent.OriginalInfoHashBytes));
        await _peerReceivingSubscriber.WriteAsync(new(torrent.OriginalInfoHashBytes, peerAdditionChannel.Writer));
        new Thread(async () =>
        {
            await using var _ = peerManager;
            try
            {
                await peerManager.ListenAsync(peerAdditionChannel.Reader, removalChannel.Reader, cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.LogError("Error in peer manager: {}", e);
            }
        }).Start();
    }

    public async Task RemoveDownload(int id)
    {
        var download = _downloads[id];
        _downloads.RemoveAt(id);
        download.CancellationTokenSource.Cancel();
        await _peerReceivingSubscriber.WriteAsync(new(download.InfoHash, null));
    }

    public IEnumerable<DownloadUpdate> GetUpdates()
    {
        return _downloads.Select(d => d.UpdateProvider.GetUpdate());
    }

    public async ValueTask DisposeAsync()
    {
        while (_downloads.Count != 0)
        {
            await RemoveDownload(_downloads.Count - 1);
        }
    }

    public void Dispose()
    {
        while (_downloads.Count != 0)
        {
            _ = RemoveDownload(_downloads.Count - 1);
        }
    }
}
