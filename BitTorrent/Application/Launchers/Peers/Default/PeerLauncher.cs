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
using System.Threading.Channels;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Launchers.Peers.Default;
public class PeerLauncher : IPeerLauncher
{
    private readonly Downloader _downloader;
    private readonly BlockStorage _storage;
    private readonly ChannelWriter<DownloadExecutionState> _downloadStateWriter;

    public PeerLauncher(Downloader downloader, BlockStorage storage, ChannelWriter<DownloadExecutionState> downloadStateWriter)
    {
        _downloader = downloader;
        _storage = storage;
        _downloadStateWriter = downloadStateWriter;
    }

    public async Task LaunchPeer(PeerWireStream stream, PeerState state, ChannelReader<DataTransferVector> relationReader, ChannelReader<int> haveReader, CancellationToken cancellationToken = default)
    {
        await using var _ = stream;
        var distributor = new BlockDistributor(_downloader, _storage, _downloadStateWriter);
        var messageChannel = Channel.CreateBounded<IMemoryOwner<Message>>(16);
        var cancellationCannel = Channel.CreateBounded<BlockRequest>(16);
        var sender = new MessageSenderProxy(messageChannel.Writer, cancellationCannel.Writer);
        var writer = new MessageWriter(stream.Sender, state);
        var writingEventHandler = new MessageWritingEventHandler(writer);
        var writingEventListener = new MessageWritingEventListener(writingEventHandler, messageChannel.Reader, cancellationCannel.Reader);
        var peer = new Peer(state, distributor, sender);
        var eventHandler = new PeerEventHandler(peer, _downloader.Torrent.PieceSize);
        var eventListener = new PeerEventListener(eventHandler, stream.Reader, haveReader, relationReader);
        await Task.WhenAll(eventListener.ListenAsync(cancellationToken), writingEventListener.ListenAsync(cancellationToken));
    }
}
