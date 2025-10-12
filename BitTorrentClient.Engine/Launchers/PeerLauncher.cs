using System.Threading.Channels;
using BitTorrentClient.Engine.Events.Handling;
using BitTorrentClient.Engine.Events.Listening;
using BitTorrentClient.Engine.Events.Listening.Interface;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.MessageWriting;
using BitTorrentClient.Engine.Infrastructure.Peers;
using BitTorrentClient.Engine.Launchers.Interface;
using BitTorrentClient.Engine.Models.Messages;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Engine.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Engine.Launchers;
public sealed class PeerLauncher : IPeerLauncher
{
    private readonly SynchronizedBlockAssigner _downloader;
    private readonly DownloadState _state;
    private readonly ChannelWriter<ReadOnlyMemory<byte>?> _peerRemovalWriter;
    private readonly BlockStorage _storage;
    private readonly ILogger _logger;
    private readonly int _pieceCount;
    private readonly TimeSpan _keepAliveInterval;

    public PeerLauncher(SynchronizedBlockAssigner downloader, DownloadState state, ChannelWriter<ReadOnlyMemory<byte>?> peerRemovalWriter, int pieceCount, TimeSpan keepAliveInterval, BlockStorage storage, ILogger logger)
    {
        _state = state;
        _downloader = downloader;
        _peerRemovalWriter = peerRemovalWriter;
        _storage = storage;
        _logger = logger;
        _pieceCount = pieceCount;
        _keepAliveInterval = keepAliveInterval;
    }

    public PeerHandle LaunchPeer(PeerWireStream stream)
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

        var distributor = new BlockDistributor(_downloader, _state, _storage);
        var peer = new Peer(state, distributor, sender);
        var eventHandler = new PeerEventHandler(peer, _state.Download.Data.PieceSize);
        var eventListener = new PeerEventListener(eventHandler, stream.Reader, haveChannel.Reader, relationChannel.Reader);

        var canceller = new CancellationTokenSource();
        _ = StartPeer(stream, eventListener, writingEventListener, canceller.Token);
        return new(state, haveChannel.Writer, relationChannel.Writer, canceller);
    }

    private async Task StartPeer(PeerWireStream stream, PeerEventListener peerEventListener, MessageWritingEventListener writingEventListener, CancellationToken cancellationToken)
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
            await _peerRemovalWriter.WriteAsync(stream.ReceivedHandshake.PeerId, cancellationToken);
        }
    }
}
