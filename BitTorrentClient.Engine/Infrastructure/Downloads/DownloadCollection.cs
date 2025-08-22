using System.Collections.Concurrent;
using BitTorrentClient.Engine.Infrastructure.Downloads.Interface;
using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Presentation.Torrent;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using BitTorrentClient.Protocol.Transport.Trackers;

namespace BitTorrentClient.Engine.Infrastructure.Downloads;

public class DownloadCollection : IDownloadRepository
{
    private readonly ConcurrentDictionary<DownloadId, PeerManagerHandle> _downloads;
    private readonly string _clientId;
    private readonly Config _config;
    private readonly ITrackerFinder _trackerFinder;
    private readonly IDownloadLauncher _launcher;

    public DownloadCollection(string clientId, Config config, ITrackerFinder trackerFinder, IDownloadLauncher launcher)
    {
        _clientId = clientId;
        _config = config;
        _trackerFinder = trackerFinder;
        _launcher = launcher;
        _downloads = new();
    }

    public async Task<DownloadHandle> AddDownloadAsync(DownloadData data, StorageStream storage, CancellationToken cancellationToken = default)
    {
        var tracker = await _trackerFinder.FindTrackerAsync(data.Trackers);
        var download = new Download(_clientId, data, _config);
        var handle = _launcher.LaunchDownload(download, storage, tracker);
        _downloads.TryAdd(new(data.InfoHash), handle);
        return new(handle.State, handle.StateWriter);
    }

    public async Task<bool> RemoveDownloadAsync(DownloadId id)
    {
        if (_downloads.TryRemove(id, out var handle))
        {
            await handle.Canceller.CancelAsync();
            return true;
        }

        return false;
    }

    public IEnumerable<DownloadHandle> GetDownloads()
    {
        return _downloads.Values.Select(handle => new DownloadHandle(handle.State, handle.StateWriter));
    }

    public DownloadHandle GetDownload(DownloadId id)
    {
        var handle = _downloads[id];
        return new(handle.State, handle.StateWriter);
    }

    public async Task AddPeerAsync(PendingPeerWireStream<InitialReadDataPhase> peer,
        CancellationToken cancellationToken = default)
    {
        var sender = await peer.ReadDataAsync(cancellationToken);
        if (_downloads.TryGetValue(new(sender.GetReceivedHandshake().InfoHash), out var download))
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
