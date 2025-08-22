using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Models.Trackers;
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
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using System.Collections.Concurrent;
using BitTorrentClient.Helpers;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public class DownloadCollection : IAsyncDisposable, IDownloadCollection
{
    private readonly ConcurrentDictionary<ReadOnlyMemory<byte>, PeerManagerHandle> _downloads = new(new MemoryComparer<byte>());
    private readonly PeerIdGenerator _peerIdGenerator;
    private readonly Config _config;
    private readonly ILogger _logger;
    private readonly ITrackerFinder _trackerFinder;

    public DownloadCollection(PeerIdGenerator peerIdGenerator, Config config, ILogger logger, ITrackerFinder trackerFinder)
    {
        _peerIdGenerator = peerIdGenerator;
        _config = config;
        _logger = logger;
        _trackerFinder = trackerFinder;
    }
    
    public async Task AddDownloadAsync(Torrent torrent, DownloadStorage storage, CancellationToken cancellationToken = default)
    {
        string peerId = _peerIdGenerator.GeneratePeerId();
        var fetcher = await _trackerFinder.FindTrackerAsync(torrent.Trackers);
        var download = new Download(peerId, torrent.DisplayName, torrent, _config);
        var downloadState = new DownloadState(download, storage);
        var peerAdditionChannel = Channel.CreateBounded<IHandshakeSender<IBitfieldSender>>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        var removalChannel = Channel.CreateBounded<int?>(new BoundedChannelOptions(8)
        {
            SingleWriter = false
        });
        
        var spawner = new PeerSpawner(downloadState, _logger, removalChannel.Writer, peerAdditionChannel.Writer, Encoding.ASCII.GetBytes(peerId));
        var peers = new PeerCollection(spawner, torrent.NumberOfPieces, _config.MaxParallelPeers);
        await using var peerManager = new PeerManager(peerId, download, peers, _logger, fetcher);
        var cancellationTokenSource = new CancellationTokenSource();
        _downloads.TryAdd(torrent.OriginalInfoHashBytes, new(peerManager, cancellationTokenSource, torrent.OriginalInfoHashBytes, peerAdditionChannel.Writer));
        try
        {
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError("Error in peer manager: {}", e);
        }
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

    public async Task AddPeerAsync(IHandshakeReceiver<IRespondedHandshakeSender<IBitfieldSender>> peer)
    {
        var sender = await peer.ReadHandShakeAsync();
        if (_downloads.TryGetValue(sender.ReceiveHandshake.InfoHash, out var download))
        {
            await download.PeerSender.WriteAsync(sender);
        }
    }
}
