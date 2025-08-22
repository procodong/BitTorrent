using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using System.Collections.Concurrent;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers;
using BitTorrentClient.Helpers.Utility;
using BitTorrentClient.Application.Infrastructure.Downloads.Interface;
using BitTorrentClient.Application.Launchers.Interface;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
internal class DownloadCollection : IDownloadCollection
{
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, PeerManagerHandle> _downloads;
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
        _downloads = new(new MemoryComparer<byte>());
    }

    public async Task<IDownloadController> AddDownloadAsync(DownloadData data, StorageStream storage, CancellationToken cancellationToken = default)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var tracker = await _trackerFinder.FindTrackerAsync(data.Trackers);
        var download = new Download(peerId, data, _config);
        var handle = _launcher.LaunchDownload(download, storage, tracker);
        _downloads.TryAdd(data.InfoHash, handle);
        return handle.Controller;
    }

    public async Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id)
    {
        if (_downloads.TryRemove(id, out var handle))
        {
            await handle.Canceller.CancelAsync();
            return true;
        }

        return false;
    }

    public IEnumerable<IDownloadController> GetDownloads()
    {
        return _downloads.Values.Select(h => h.Controller);
    }

    public IDownloadController GetDownloadController(ReadOnlyMemory<byte> id)
    {
        return _downloads[id].Controller;
    }

    public async Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender<PeerWireStream>>> peer,
        CancellationToken cancellationToken = default)
    {
        var sender = await peer.ReadHandShakeAsync(cancellationToken);
        if (_downloads.TryGetValue(sender.ReceiveHandshake.InfoHash, out var download))
        {
            await download.PeerSpawner.SpawnConnect(sender, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var download in _downloads)
        {
            await download.Value.Canceller.CancelAsync();
        }
        _downloads.Clear();
    }

    public void Dispose()
    {
        foreach (var download in _downloads)
        {
            download.Value.Canceller.Cancel();
        }
        _downloads.Clear();
    }
}
