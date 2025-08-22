using BitTorrentClient.Application.Events.Handling.MessageWriting;
using BitTorrentClient.Application.Events.Handling.Peers;
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
using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening;
using BitTorrentClient.Application.Infrastructure.PeerManagement;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Launchers.Peers.Default;
public class PeerLauncher : IPeerLauncher
{
    private readonly Downloader _downloader;
    private readonly ChannelWriter<int?> _peerRemovalWriter;
    private readonly BlockStorage _storage;
    private readonly ILogger _logger;
    private readonly int _pieceCount;

    public PeerLauncher(Downloader downloader, ChannelWriter<int?> peerRemovalWriter, BlockStorage storage, ILogger logger)
    {
        _downloader = downloader;
        _peerRemovalWriter = peerRemovalWriter;
        _storage = storage;
        _logger = logger;
    }
    
    private async Task StartPeer(IEventListener peerEventListener, CancellationToken cancellationToken)
    {
        try
        {
            
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ex is BadPeerException)
            {
                _logger.LogError("peer connection {}", ex);
            }
            else if (ex is not SocketException or IOException)
            {
                _logger.LogError("peer connection {}", ex);
            }
        }
        finally
        {
            await _peerRemovalWriter.WriteAsync(peerIndex, cancellationToken);
        }
    }

    public PeerHandle LaunchPeer(PeerWireStream stream, int peerIndex)
    {
        var state = new PeerState(new(new(_pieceCount)), new(long.MaxValue, long.MaxValue));
        var haveChannel = Channel.CreateBounded<int>(16);
        var relationChannel = Channel.CreateBounded<DataTransferVector>(16);
        var messageChannel = Channel.CreateBounded<IMemoryOwner<Message>>(8);
        var cancellationCannel = Channel.CreateBounded<BlockRequest>(8);
        var sender = new MessageSenderProxy(messageChannel.Writer, cancellationCannel.Writer);
        var writer = new MessageWriter(stream.Sender, state);
        var writingEventHandler = new MessageWritingEventHandler(writer);
        var writingEventListener = new MessageWritingEventListener(writingEventHandler, messageChannel.Reader, cancellationCannel.Reader);
        var distributor = new BlockDistributor(_downloader, _storage);
        var peer = new Peer(state, distributor, sender);
        var eventHandler = new PeerEventHandler(peer, _downloader.Torrent.PieceSize);
        var eventListener = new PeerEventListener(eventHandler, stream.Reader, haveChannel.Reader, relationChannel.Reader);
        var canceller = new CancellationTokenSource();
        _ = StartPeer(stream, state, relationChannel.Reader, haveChannel.Reader, peerIndex, canceller.Token);
        return new(state, haveChannel.Writer, relationChannel.Writer, canceller);
    }
}
