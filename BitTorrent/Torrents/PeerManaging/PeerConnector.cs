using BitTorrent.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public readonly record struct PeerConnector(PeerStatistics Stats, ChannelWriter<PeerManagerEvent> EventWriter);