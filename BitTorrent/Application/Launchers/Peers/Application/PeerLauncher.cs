using BitTorrentClient.Application.Events.Handling.MessageWriting;
using BitTorrentClient.Application.Events.Handling.Peers;
using BitTorrentClient.Application.Events.Listening.MessageWriting;
using BitTorrentClient.Application.Events.Listening.Peers;
using BitTorrentClient.Application.Infrastructure.MessageWriting;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using System.Buffers;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Launchers.Peers.Application;
public class PeerLauncher : IPeerLauncher
{
    public async Task LaunchPeer(PeerWireStream stream, PeerState state, IBlockRequester blockRequester, ChannelReader<PeerRelation> relationReader, ChannelReader<int> haveReader, CancellationToken cancellationToken = default)
    {
        var messageChannel = Channel.CreateBounded<IMemoryOwner<Message>>(16);
        var cancellationCannel = Channel.CreateBounded<PieceRequest>(16);
        var sender = new MessageSenderProxy(messageChannel.Writer, cancellationCannel.Writer);
        var writer = new MessageWriter(stream.Sender, state);
        var writingEventHandler = new MessageWritingEventHandler(writer);
        var writingEventListener = new MessageWritingEventListener(writingEventHandler, messageChannel.Reader, cancellationCannel.Reader);
        var peer = new Peer(state, blockRequester, sender);
        var eventHandler = new PeerEventHandler(peer);
        var eventListener = new PeerEventListener(eventHandler, stream.Reader, haveReader, relationReader);
        await Task.WhenAll(eventListener.ListenAsync(cancellationToken), writingEventListener.ListenAsync(cancellationToken));
    }
}
