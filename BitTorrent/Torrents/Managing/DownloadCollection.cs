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
        var peerManager = new PeerManager(peerId, download, _logger, fetcher);
        var completer = new TaskCompletionSource();
        _downloads.Add(new(peerManager, completer, torrent.OriginalInfoHashBytes));
        var trackerChannel = Channel.CreateBounded<IdentifiedPeerWireStream>(8);
        await _peerReceivingSubscriber.WriteAsync(new(torrent.OriginalInfoHashBytes, trackerChannel.Writer));
        new Thread(async () =>
        {
            await using var _ = peerManager;
            try
            {
                await peerManager.ListenAsync(trackerChannel.Reader, completer);
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
        download.Completion.SetCanceled();
        await _peerReceivingSubscriber.WriteAsync(new(download.InfoHash, null));
    }

    public IEnumerable<DownloadUpdate> GetUpdates()
    {
        foreach (var download in _downloads)
        {
            yield return download.UpdateProvider.GetUpdate();
        }
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
