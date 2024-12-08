using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;
public record class PeerConnector(
    SharedPeerState Data, 
    ChannelWriter<PeerRelation> RelationEventWriter,
    DataTransferVector LastStatistics,
    DataTransferCounter LastUnchokedStats,
    CancellationTokenSource Canceller
    )
{
    public DataTransferVector LastStatistics = LastStatistics;
}
