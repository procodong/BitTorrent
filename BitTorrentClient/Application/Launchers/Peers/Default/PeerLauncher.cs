using BitTorrentClient.Application.Events.Listening.MessageWriting;
using BitTorrentClient.Application.Events.Listening.Peers;
using BitTorrentClient.Application.Infrastructure.MessageWriting;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using System.Buffers;
using System.Threading.Channels;
using BitTorrentClient.Application.Events.Handling;
using BitTorrentClient.Application.Events.Listening;
using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Launchers.Peers.Default;
public class PeerLauncher : IPeerLauncher
{
    private readonly Downloader _downloader;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly BlockStorage _storage;
    private readonly ILogger _logger;
    private readonly int _pieceCount;

    public PeerLauncher(Downloader downloader, ChannelWriter<int?> peerRemovalWriter, int pieceCount, BlockStorage storage, ILogger logger)
    {
        _downloader = downloader;
        _peerRemovalWriter = peerRemovalWriter;
        _storage = storage;
        _logger = logger;
        _pieceCount = pieceCount;
    }

    public PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex)
    {
        var haveChannel = Channel.CreateBounded<int>(8);
        var relationChannel = Channel.CreateBounded<DataTransferVector>(8);
        var messageChannel = Channel.CreateBounded<IMemoryOwner<Message>>(8);
        var cancellationCannel = Channel.CreateBounded<BlockRequest>(8);

        var state = new PeerState(new(_pieceCount), new(long.MaxValue, long.MaxValue));

        var sender = new MessageSenderProxy(messageChannel.Writer, cancellationCannel.Writer);
        var writer = new MessageWriter(stream.Sender, state);
        var writingEventHandler = new MessageWritingEventHandler(writer);
        var writingEventListener = new MessageWritingEventListener(writingEventHandler, messageChannel.Reader, cancellationCannel.Reader);

        var distributor = new BlockDistributor(_downloader, _storage);
        var peer = new Peer(state, distributor, sender);
        var eventHandler = new PeerEventHandler(peer, _downloader.Torrent.PieceSize);
        var eventListener = new PeerEventListener(eventHandler, stream.Reader, haveChannel.Reader, relationChannel.Reader);

        var canceller = new CancellationTokenSource();
        _ = StartPeer(stream, eventListener, writingEventListener, peerIndex, canceller.Token);
        return new(state, haveChannel.Writer, relationChannel.Writer, canceller);
    }

    private async Task StartPeer(PeerWireStream stream, IEventListener peerEventListener, IEventListener writingEventListener, int peerIndex, CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(peerEventListener.ListenAsync(cancellationToken), writingEventListener.ListenAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "peer connection {}", ex);
        }
        finally
        {
            await stream.DisposeAsync();
            await _peerRemovalWriter.WriteAsync(peerIndex, cancellationToken);
        }
    }
}
