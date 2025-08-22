using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;
public interface IDelayedMessageSender
{
    void SendRelation(RelationUpdate relation);
    void SendHave(int piece);
    void SendRequest(BlockRequest request);
    void SendCancel(BlockRequest cancel);
    void SendKeepAlive();
    Task SendBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task SendBlockAsync(IPieceDelayer delayer, CancellationToken cancellationToken = default);
    bool TryCancelUpload(BlockRequest request);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
