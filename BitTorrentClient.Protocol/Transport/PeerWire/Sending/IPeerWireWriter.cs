using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Transport.PeerWire.Sending;
public interface IMessageSender
{
    void SendRelation(RelationUpdate relation);
    void SendHave(int piece);
    void SendRequest(BlockRequest request);
    void SendCancel(BlockRequest cancel);
    void SendKeepAlive();
    void TryCancelUpload(BlockRequest cancel);
    Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
