using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using System.Collections.Concurrent;
using BitTorrentClient.Helpers;
using BitTorrentClient.Application.Launchers.Downloads;
using BitTorrentClient.Application.Events.Handling.Downloads;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public class DownloadCollection : IAsyncDisposable, IDisposable, IDownloadCollection
{
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, PeerManagerHandle> _downloads = new(new MemoryComparer<byte>());
    private readonly PeerIdGenerator _peerIdGenerator;
    private readonly Config _config;
    private readonly ITrackerFinder _trackerFinder;
    private readonly IDownloadLauncher _launcher;

    public DownloadCollection(PeerIdGenerator peerIdGenerator, Config config, ITrackerFinder trackerFinder, IDownloadLauncher launcher)
    {
        _peerIdGenerator = peerIdGenerator;
        _config = config;
        _trackerFinder = trackerFinder;
        _launcher = launcher;
    }
    
    public async Task AddDownloadAsync(Torrent torrent, DownloadStorage storage, CancellationToken cancellationToken = default)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var tracker = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(peerId, torrent.DisplayName, torrent, _config);
        var downloadState = new DownloadState(download);
        var handle = _launcher.LaunchDownload(downloadState, storage, tracker);
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

    public async Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer)
    {
        var sender = await peer.ReadHandShakeAsync();
        if (_downloads.TryGetValue(sender.ReceiveHandshake.InfoHash, out var download))
        {
            await download.PeerSpawner.SpawnConnect(sender);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var download in _downloads)
        {
            await download.Value.Canceller.CancelAsync();
        }
    }

    public void Dispose()
    {
        foreach (var download in _downloads)
        {
            download.Value.Canceller.Cancel();
        }
    }
}
