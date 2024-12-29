using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Input;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Storage;
using BitTorrentClient.Torrents.Managing;
using BitTorrentClient.Torrents.Peers;
using BitTorrentClient.Torrents.Trackers;
using Microsoft.Extensions.Logging;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;

namespace BitTorrentClient.Torrents.Downloads;
public class DownloadCollection : IAsyncDisposable, IDisposable, ICommandContext
{
    private readonly List<PeerManagerConnector> _downloads = [];
    private readonly PeerIdGenerator _peerIdGenerator;
    private readonly Config _config;
    private readonly ChannelWriter<PeerReceivingSubscribe> _peerReceivingSubscriber;
    private readonly DownloadStorageFactory _storageFactory;
    private readonly ILogger _logger;
    private readonly ITrackerFinder _trackerFinder;
    public bool HasUpdates => _downloads.Count != 0;
    public Config Config => _config;
    public ILogger Logger => _logger;

    public DownloadCollection(ChannelWriter<PeerReceivingSubscribe> peerReceivingSubscriber, PeerIdGenerator peerIdGenerator, DownloadStorageFactory storageFactory, Config config, ILogger logger, ITrackerFinder trackerFinder)
    {
        _peerIdGenerator = peerIdGenerator;
        _config = config;
        _logger = logger;
        _peerReceivingSubscriber = peerReceivingSubscriber;
        _trackerFinder = trackerFinder;
        _storageFactory = storageFactory;
    }
    
    public async Task StartDownloadAsync(Torrent torrent, DownloadStorage files)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var fetcher = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(torrent, files, _config);
        var peerAdditionChannel = Channel.CreateBounded<IdentifiedPeerWireStream>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var removalChannel = Channel.CreateBounded<int?>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var spawner = new PeerSpawner(download, _logger, removalChannel.Writer, peerAdditionChannel.Writer, System.Text.Encoding.ASCII.GetBytes(peerId));
        var peers = new PeerCollection(spawner, torrent.NumberOfPieces, _config.MaxParallelPeers);
        var peerManager = new PeerManager(peerId, download, peers, _logger, fetcher);
        var cancellationTokenSource = new CancellationTokenSource();
        _downloads.Add(new(peerManager, cancellationTokenSource, torrent.OriginalInfoHashBytes));
        await _peerReceivingSubscriber.WriteAsync(new(torrent.OriginalInfoHashBytes, peerAdditionChannel.Writer));
        _ = SpawnDownload(peerManager, peerAdditionChannel.Reader, removalChannel.Reader, cancellationTokenSource.Token).ConfigureAwait(false);

    }

    private async Task SpawnDownload(PeerManager download, ChannelReader<IdentifiedPeerWireStream> peerAdditionReader, ChannelReader<int?> removalReader, CancellationToken cancellationToken = default)
    {
        await using var _ = download;
        try
        {
            await download.ListenAsync(peerAdditionReader, removalReader, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError("Error in peer manager: {}", e);
        }
    }

    public async Task StopDownloadAsync(int id)
    {
        var download = _downloads[id];
        _downloads.RemoveAt(id);
        await download.Canceller.CancelAsync();
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
            await StopDownloadAsync(_downloads.Count - 1);
        }
    }

    public void Dispose()
    {
        while (_downloads.Count != 0)
        {
            _ = StopDownloadAsync(_downloads.Count - 1);
        }
    }

    async Task ICommandContext.AddTorrentAsync(string torrentPath, string targetPath)
    {
        await using var file = File.Open(torrentPath, new FileStreamOptions()
        {
            Options = FileOptions.Asynchronous,
        });
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream);
        DownloadStorage storage = torrent.Files is not null
            ? _storageFactory.CreateMultiFileStorage(targetPath, torrent.Files, (int)torrent.PieceSize)
            : _storageFactory.CreateSingleFileStorage(targetPath, torrent.File, (int)torrent.PieceSize);
        await StartDownloadAsync(torrent, storage);
    }

    async Task ICommandContext.RemoveTorrentAsync(int index)
    {
        await StopDownloadAsync(index);
    }
}
