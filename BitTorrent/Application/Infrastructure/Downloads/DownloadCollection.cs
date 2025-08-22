using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Networking.Trackers;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using System.Collections.Concurrent;
using BitTorrentClient.Helpers;
using BitTorrentClient.Application.Events.EventHandling.Downloads;
using BitTorrentClient.Application.Launchers.Downloads;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public class DownloadCollection : IAsyncDisposable, IDownloadCollection
{
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, PeerManagerHandle> _downloads = new(new MemoryComparer<byte>());
    private readonly PeerIdGenerator _peerIdGenerator;
    private readonly Config _config;
    private readonly ILogger _logger;
    private readonly ITrackerFinder _trackerFinder;
    private readonly IDownloadLauncher _launcher;

    public DownloadCollection(PeerIdGenerator peerIdGenerator, Config config, ILogger logger, ITrackerFinder trackerFinder, IDownloadLauncher launcher)
    {
        _peerIdGenerator = peerIdGenerator;
        _config = config;
        _logger = logger;
        _trackerFinder = trackerFinder;
        _launcher = launcher;
    }
    
    public async Task AddDownloadAsync(Torrent torrent, DownloadStorage storage, CancellationToken cancellationToken = default)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var tracker = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(peerId, torrent.DisplayName, torrent, _config);
        var downloadState = new DownloadState(download, storage);
        var handle = _launcher.LaunchDownload(downloadState, tracker);
        _downloads.TryAdd(torrent.OriginalInfoHashBytes, handle);
    }

    public async Task RemoveDownloadAsync(ReadOnlyMemory<byte> id)
    {
        if (_downloads.Remove(id, out var download))
        {
            await download.Canceller.CancelAsync();
        }
    }

    public IEnumerable<DownloadUpdate> GetUpdates()
    {
        return _downloads.Select(d => d.Value.UpdateProvider.GetUpdate());
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var download in _downloads)
        {
            await download.Value.Canceller.CancelAsync();
        }
    }

    public async Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer)
    {
        var sender = await peer.ReadHandShakeAsync();
        if (_downloads.TryGetValue(sender.ReceiveHandshake.InfoHash, out var download))
        {
            await download.PeerSpawner.SpawnConnect(sender);
        }
    }
}
