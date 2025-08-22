using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Sending;
public interface IMessageSender
{
    void SendRelation(RelationUpdate relation);
    void SendHave(int piece);
    void SendRequest(PieceRequest request);
    void SendCancel(PieceRequest cancel);
    void CancelUpload(PieceRequest cancel);
    Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
