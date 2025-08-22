using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using System.Collections.Concurrent;
using BitTorrentClient.Helpers;
using BitTorrentClient.Application.Launchers.Downloads;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers;
using BitTorrentClient.Helpers.Extensions;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public class DownloadCollection : IDownloadCollection
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
    
    public async Task AddDownloadAsync(Torrent torrent, StorageStream storage, string? name, CancellationToken cancellationToken = default)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var tracker = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(peerId, name ?? torrent.DisplayName, torrent, _config);
        var handle = _launcher.LaunchDownload(download, storage, tracker);
        _downloads.TryAdd(torrent.OriginalInfoHashBytes, handle);
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

    public IEnumerable<DownloadUpdate> GetDownloadState()
    {
        return _downloads.Select(d => d.Value.UpdateProvider.GetUpdate());
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
    }

    public void Dispose()
    {
        foreach (var download in _downloads)
        {
            download.Value.Canceller.Cancel();
        }
    }
}
