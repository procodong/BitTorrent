using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling;
using BitTorrentClient.Engine.Events.Listening;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Engine.Infrastructure.MessageWriting;
using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Infrastructure.Storage.Distribution;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models.Messages;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Launchers;
public class PeerLauncher : IPeerLauncher
{
    private readonly Downloader _downloader;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly BlockStorage _storage;
    private readonly ILogger _logger;
    private readonly int _pieceCount;
    private readonly TimeSpan _keepAliveInterval;

    public PeerLauncher(Downloader downloader, ChannelWriter<int?> peerRemovalWriter, int pieceCount, TimeSpan keepAliveInterval, BlockStorage storage, ILogger logger)
    {
        _downloader = downloader;
        _peerRemovalWriter = peerRemovalWriter;
        _storage = storage;
        _logger = logger;
        _pieceCount = pieceCount;
        _keepAliveInterval = keepAliveInterval;
    }

    public PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex)
    {
        var haveChannel = Channel.CreateBounded<int>(8);
        var relationChannel = Channel.CreateBounded<DataTransferVector>(8);
        var messageChannel = Channel.CreateBounded<MaybeRentedArray<Message>>(8);
        var cancellationCannel = Channel.CreateBounded<BlockRequest>(8);

        var state = new PeerState(new(_pieceCount), new(long.MaxValue, long.MaxValue));

        var sender = new MessageSenderProxy(messageChannel.Writer, cancellationCannel.Writer);
        var writer = new DelayedMessageSender(stream.Writer, state);
        var writingEventHandler = new MessageWritingEventHandler(writer);
        var writingEventListener = new MessageWritingEventListener(writingEventHandler, messageChannel.Reader, cancellationCannel.Reader, new(_keepAliveInterval));

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
