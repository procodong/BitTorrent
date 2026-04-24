using System.Collections.Concurrent;
using System.Text;
using BitTorrentClient.Engine.Infrastructure.Downloads.Interface;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;
using BitTorrentClient.Core.Transport.Trackers;
using BitTorrentClient.Engine.Models.Config;
using BitTorrentClient.Engine.Storage.Interface;
using BitTorrentClient.Engine.Storage.Strategy;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Infrastructure.Downloads;

public sealed class DownloadCollection : IDownloadRepository
{
    private readonly ConcurrentDictionary<ByteId, PeerManagerHandle> _downloads;
    private readonly ReadOnlyMemory<byte> _clientId;
    private readonly NetworkingConfig _config;
    private readonly TrackerFinder _trackerFinder;
    private readonly IDownloadLauncher _launcher;

    public DownloadCollection(string clientId, NetworkingConfig config, TrackerFinder trackerFinder, IDownloadLauncher launcher)
    {
        _clientId = Encoding.ASCII.GetBytes(clientId);
        _config = config;
        _trackerFinder = trackerFinder;
        _launcher = launcher;
        _downloads = new();
    }

    public DownloadHandle AddDownload(DownloadData data, StorageStream storage, DownloadSettings settings)
    {
        var download = new Download(_clientId, data, settings);
        var tracker = new LazyTrackerFinder(_trackerFinder, data.Trackers);
        IPieceSelectionStrategy pieceSelector = settings.Strategy switch
        {
            PieceSelectionStrategyType.RarestFirst => new RarestFirstStrategy(),
            PieceSelectionStrategyType.Sequential => new SequentialPieceStrategy()
        };
        var handle = _launcher.LaunchDownload(download, storage, tracker, _config, pieceSelector);
        _downloads.TryAdd(new(data.InfoHash), handle);
        return new(handle.State, handle.StateWriter);
    }

    public bool RemoveDownload(ByteId id)
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
