using BitTorrentClient.Models.Messages;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventListening.MessageWriting;
public interface IMessageWritingEventHandler
{
    Task OnMessageAsync(ReadOnlySequence<byte> data, CancellationToken cancellationToken = default);
    Task OnBlockAsync(BlockData data, CancellationToken cancellationToken = default);
    Task OnCancelAsync(PieceRequest cancel, CancellationToken cancellationToken = default);
}
