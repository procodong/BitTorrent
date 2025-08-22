using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
using BitTorrentClient.Storage;
using BitTorrentClient.BitTorrent.Managing;
using Microsoft.Extensions.Logging;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using BitTorrentClient.Application.EventHandling.Downloads;
using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.UserInterface.Input;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.Trackers;
using PeerManager = BitTorrentClient.BitTorrent.Managing.PeerManager;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public class DownloadCollection : IAsyncDisposable, IDownloadCollection
{
    private readonly List<PeerManagerConnector> _downloads = [];
    private readonly PeerIdGenerator _peerIdGenerator;
    private readonly Config _config;
    private readonly ChannelWriter<PeerReceivingSubscribe> _peerReceivingSubscriber;
    private readonly ILogger _logger;
    private readonly ITrackerFinder _trackerFinder;
    public bool HasUpdates => _downloads.Count != 0;
    public Config Config => _config;
    public ILogger Logger => _logger;

    public DownloadCollection(ChannelWriter<PeerReceivingSubscribe> peerReceivingSubscriber, PeerIdGenerator peerIdGenerator, Config config, ILogger logger, ITrackerFinder trackerFinder)
    {
        _peerIdGenerator = peerIdGenerator;
        _config = config;
        _logger = logger;
        _peerReceivingSubscriber = peerReceivingSubscriber;
        _trackerFinder = trackerFinder;
    }
    
    public async Task AddDownloadAsync(Torrent torrent, DownloadStorage storage, CancellationToken cancellationToken = default)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var fetcher = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(torrent, storage, _config);
        var peerAdditionChannel = Channel.CreateBounded<RespondedPeerHandshaker>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var removalChannel = Channel.CreateBounded<int?>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        
        var spawner = new PeerSpawner(download, _logger, removalChannel.Writer, peerAdditionChannel.Writer, Encoding.ASCII.GetBytes(peerId));
        var peers = new PeerCollection(spawner, torrent.NumberOfPieces, _config.MaxParallelPeers);
        var peerManager = new PeerManager(peerId, download, peers, _logger, fetcher);
        var cancellationTokenSource = new CancellationTokenSource();
        _downloads.Add(new(peerManager, cancellationTokenSource, torrent.OriginalInfoHashBytes));
        await _peerReceivingSubscriber.WriteAsync(new(torrent.OriginalInfoHashBytes, peerAdditionChannel.Writer));
        _ = SpawnDownload(peerManager, peerAdditionChannel.Reader, removalChannel.Reader, cancellationTokenSource.Token).ConfigureAwait(false);
    }
    
    private async Task SpawnDownload(PeerManager download, ChannelReader<RespondedPeerHandshaker> peerAdditionReader, ChannelReader<int?> removalReader, CancellationToken cancellationToken = default)
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


    public async Task RemoveDownloadAsync(int index)
    {
        var download = _downloads[index];
        _downloads.RemoveAt(index);
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
            await RemoveDownloadAsync(_downloads.Count - 1);
        }
    }
}
