using BitTorrentClient.Models.Peers;
using System.Threading.Channels;

namespace BitTorrentClient.Application.Infrastructure.PeerManagement;
public class PeerHandle
{
    public DataTransferVector LastStatistics { get; set; }
    public DataTransferVector LastUnchokedStats { get; set; }
    public PeerState State { get; }
    public ChannelWriter<DataTransferVector> RelationEventWriter { get; }
    public ChannelWriter<int> HaveEventWriter { get; }
    public CancellationTokenSource Canceller { get; }


    public PeerHandle(PeerState state, ChannelWriter<int> haveWriter, ChannelWriter<DataTransferVector> relationEventWriter, CancellationTokenSource canceller)
    {
        HaveEventWriter = haveWriter;
        State = state;
        RelationEventWriter = relationEventWriter;
        Canceller = canceller;
    }
}
