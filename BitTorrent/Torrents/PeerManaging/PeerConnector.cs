using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public record class PeerConnector(
    SharedPeerData Data, 
    ChannelWriter<Relation> RelationEventWriter,
    PeerStatistics LastStatistics
    );