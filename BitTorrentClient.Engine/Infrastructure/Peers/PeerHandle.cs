using System.Threading.Channels;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
public sealed class PeerHandle
{
    public DataTransferVector LastStatistics { get; set; }
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
