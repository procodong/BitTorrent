using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Application.Infrastructure.Peers;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerHandle
{
    public DataTransferVector LastStatistics { get; set; }
    public DataTransferVector LastUnchokedStats { get; set; }
    public PeerState State { get; set; }
    public ChannelWriter<PeerRelation> RelationEventWriter { get; set; }
    public CancellationTokenSource Canceller { get; set; }


    public PeerHandle(PeerState state, ChannelWriter<PeerRelation> relationEventWriter, CancellationTokenSource canceller)
    {
        State = state;
        RelationEventWriter = relationEventWriter;
        Canceller = canceller;
    }
}
