using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire.Handshakes;

namespace BitTorrentClient.Models.Trackers;
public readonly record struct PeerReceivingSubscribe(ReadOnlyMemory<byte> InfoHash, ChannelWriter<IHandshakeSender<IBitfieldSender>>? EventWriter);