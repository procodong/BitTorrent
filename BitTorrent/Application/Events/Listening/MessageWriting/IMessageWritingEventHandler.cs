using BitTorrentClient.Models.Messages;
using System.Buffers;
using BitTorrentClient.Application.Events.Handling.MessageWriting;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;
public interface IMessageWritingEventHandler
{
    Task OnMessageAsync(IMemoryOwner<Message> message, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task OnCancelAsync(PieceRequest cancel, CancellationToken cancellationToken = default);
    Task OnDelayEnd(IPieceDelayer delayer, CancellationToken cancellationToken = default);
}
