using BitTorrentClient.Models.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Events.Handling.MessageWriting;
public interface IMessageWriter
{
    Task WriteMessageAsync(Message message, IPieceDelayer pieceDelayer, CancellationToken cancellationToken = default);
    Task WriteQueuedBlock(CancellationToken cancellationToken = default);
    void RemoveQueuedBlock(PieceRequest request);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
