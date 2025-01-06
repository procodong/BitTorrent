using BitTorrentClient.Models.Peers;
using BitTorrentClient.BitTorrent.Peers;
using BitTorrentClient.BitTorrent.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Trackers;
public readonly record struct PeerReceivingSubscribe(ReadOnlyMemory<byte> InfoHash, ChannelWriter<IdentifiedPeerWireStream>? EventWriter);