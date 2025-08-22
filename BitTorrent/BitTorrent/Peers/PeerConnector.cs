using BitTorrentClient.BitTorrent.Peers.Connections;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.BitTorrent.Peers;
public class PeerConnector(
    PeerState data,
    ChannelWriter<PeerRelation> relationEventWriter,
    CancellationTokenSource canceller,
    DataTransferVector lastStatistics = default,
    DataTransferVector lastUnchokedStats = default
    )
{
    public DataTransferVector LastStatistics = lastStatistics;
    public DataTransferVector LastUnchokedStats = lastUnchokedStats;
    public readonly PeerState State = data;
    public readonly ChannelWriter<PeerRelation> RelationEventWriter = relationEventWriter;
    public readonly CancellationTokenSource Canceller = canceller;
}
