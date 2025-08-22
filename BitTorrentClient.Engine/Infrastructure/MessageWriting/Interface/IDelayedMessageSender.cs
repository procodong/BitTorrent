using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;
internal interface IMessageWriter
{
    void WriteRelation(RelationUpdate relation);
    void WriteHave(int piece);
    void WriteRequest(BlockRequest request);
    void WriteCancel(BlockRequest cancel);
    void WriteKeepAlive();
    Task WriteBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task WriteBlockAsync(IPieceDelayer delayer, CancellationToken cancellationToken = default);
    bool TryCancelUpload(BlockRequest request);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
