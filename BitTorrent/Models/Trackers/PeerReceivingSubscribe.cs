using System.Threading.Channels;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Models.Trackers;
public readonly record struct PeerReceivingSubscribe(ReadOnlyMemory<byte> InfoHash, ChannelWriter<IHandshakeSender<IBitfieldSender>>? EventWriter);