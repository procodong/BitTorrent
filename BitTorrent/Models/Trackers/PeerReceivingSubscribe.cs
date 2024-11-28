using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Peers;
using BitTorrent.Torrents.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Models.Trackers;
public readonly record struct PeerReceivingSubscribe(ReadOnlyMemory<byte> InfoHash, ChannelWriter<IdentifiedPeerWireStream>? EventWriter);