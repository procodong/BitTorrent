using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Infrastructure.Downloads;
public readonly record struct PeerManagerHandle(IApplicationUpdateProvider UpdateProvider, CancellationTokenSource Canceller, byte[] InfoHash, ChannelWriter<IHandshakeSender<IBitfieldSender>> PeerSender);
