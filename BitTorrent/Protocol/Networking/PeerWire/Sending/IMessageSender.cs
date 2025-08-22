using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Networking.PeerWire.Sending;
public interface IMessageSender
{
    void SendRelation(RelationUpdate relation);
    void SendHave(int piece);
    void SendRequest(PieceRequest request);
    void SendCancel(PieceRequest cancel);
    Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
