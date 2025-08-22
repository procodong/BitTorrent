using BitTorrentClient.Models.Peers;
using System.Threading.Channels;
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
