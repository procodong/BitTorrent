using System.Collections.Concurrent;
using System.Text;
using BitTorrentClient.Engine.Infrastructure.Downloads.Interface;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;
using BitTorrentClient.Core.Transport.Trackers;

namespace BitTorrentClient.Engine.Infrastructure.Downloads;

public sealed class DownloadCollection : IDownloadRepository
{
    private readonly ConcurrentDictionary<DownloadId, PeerManagerHandle> _downloads;
    private readonly ReadOnlyMemory<byte> _clientId;
    private readonly Config _config;
    private readonly TrackerFinder _trackerFinder;
    private readonly IDownloadLauncher _launcher;

    public DownloadCollection(string clientId, Config config, TrackerFinder trackerFinder, IDownloadLauncher launcher)
    {
        _clientId = Encoding.ASCII.GetBytes(clientId);
        _config = config;
        _trackerFinder = trackerFinder;
        _launcher = launcher;
        _downloads = new();
    }

    public DownloadHandle AddDownload(DownloadData data, StorageStream storage)
    {
        var download = new Download(_clientId, data, _config);
        var tracker = new LazyTrackerFinder(_trackerFinder, data.Trackers);
        var handle = _launcher.LaunchDownload(download, storage, tracker);
        _downloads.TryAdd(new(data.InfoHash), handle);
        return new(handle.State, handle.StateWriter);
    }

    public bool RemoveDownload(DownloadId id)
    {
        if (_downloads.TryRemove(id, out var handle))
        {
            handle.Canceller.Cancel();
            return true;
        }

        return false;
    }

    public IEnumerable<DownloadHandle> GetDownloads()
    {
        return _downloads.Values.Select(handle => new DownloadHandle(handle.State, handle.StateWriter));
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
        _trackerFinder.Dispose();
        foreach (var download in _downloads.Values)
        {
            await download.Canceller.CancelAsync();
        }
        await Task.WhenAll(_downloads.Values.Select(d => d.FinishedTask));
        _downloads.Clear();
    }

    public void Dispose()
    {
        _trackerFinder.Dispose();
        foreach (var download in _downloads)
        {
            download.Value.Canceller.Cancel();
        }
        Task.WhenAll(_downloads.Values.Select(d => d.FinishedTask)).GetAwaiter().GetResult();
        _downloads.Clear();
    }
}
