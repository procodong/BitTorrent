using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.Handling.MessageWriting;
public interface IMessageWriter
{
    void WriteRelation(RelationUpdate relation);
    void WriteHave(int piece);
    void WriteRequest(BlockRequest request);
    void WriteCancel(BlockRequest cancel);
    Task WriteBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task WriteBlockAsync(IPieceDelayer delayer, CancellationToken cancellationToken = default);
    bool TryCancelUpload(BlockRequest request);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
