using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Launchers.Peers;
public interface IPeerLauncher
{
    Task LaunchPeer(PeerWireStream stream, PeerState state, IBlockRequester blockRequester, ChannelReader<PeerRelation> relationReader, ChannelReader<int> haveReader, CancellationToken cancellationToken = default);
}
