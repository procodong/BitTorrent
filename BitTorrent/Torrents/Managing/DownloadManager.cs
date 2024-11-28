using BencodeNET.Torrents;
using BitTorrent.Files.DownloadFiles;
using BitTorrent.Models.Application;
using BitTorrent.Models.Peers;
using BitTorrent.Models.Tracker;
using BitTorrent.Models.Trackers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Trackers;
using BitTorrent.Torrents.Trackers.Errors;
using BitTorrent.Utils;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Managing;
public class DownloadManager
{
    private readonly SlotMap<(string Name, PeerManager Download)> _downloads = [];
    private readonly PeerIdGenerator _peerIdGenerator = new();
    private readonly Config _config;
    private readonly ChannelWriter<PeerReceivingSubscribe> _peerReceivingSubscriber;
    private readonly ILogger _logger;
    private readonly ITrackerFinder _trackerFinder;
    public bool HasUpdates => _downloads.Count != 0;

    public DownloadManager(ChannelWriter<PeerReceivingSubscribe> peerReceivingSubscriber, Config config, ILogger logger, ITrackerFinder trackerFinder)
    {
        _config = config;
        _logger = logger;
        _peerReceivingSubscriber = peerReceivingSubscriber;
        _trackerFinder = trackerFinder;
    }

    public async Task<int> StartDownload(Torrent torrent, DownloadSaveManager files, string name)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var request = new TrackerUpdate(torrent.OriginalInfoHashBytes, peerId, new(), torrent.TotalSize, TrackerEvent.Started);
        var fetcher = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(torrent, files, _config);
        var downloadManager = new PeerManager(peerId, download, _logger);
        int index = _downloads.Add((name, downloadManager));
        var trackerChannel = Channel.CreateBounded<IdentifiedPeerWireStream>(8);
        await _peerReceivingSubscriber.WriteAsync(new(torrent.OriginalInfoHashBytes, trackerChannel.Writer));
        _ = Task.Run(async () =>
        {
            await downloadManager.ListenAsync(trackerChannel.Reader, fetcher);
        });
        return index;
    }

    public async Task StopDownload(int id)
    {
        await using var download = _downloads[id].Download;
        _downloads.Remove(id);
        await _peerReceivingSubscriber.WriteAsync(new(download.InfoHash, null));
    }

    public IEnumerable<DownloadUpdate> GetUpdates()
    {
        foreach (var (name, download) in _downloads)
        {
            var transfer = download.Transfered;
            yield return new(name, transfer, download.TransferRate, download.Size);
        }
    }
}
