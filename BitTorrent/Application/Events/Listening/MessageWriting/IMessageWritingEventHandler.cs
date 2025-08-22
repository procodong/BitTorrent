using BitTorrentClient.Models.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTorrentClient.Application.Events.EventHandling.MessageWriting;

namespace BitTorrentClient.Application.Events.EventListening.MessageWriting;
public interface IMessageWritingEventHandler
{
    Task OnMessageAsync(ReadOnlyMemory<Message> message, CancellationToken cancellationToken = default);
    Task OnBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task OnCancelAsync(PieceRequest cancel, CancellationToken cancellationToken = default);
}
