using BitTorrentClient.Models.Messages;
using System.Buffers;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;
public interface IMessageWritingEventHandler
{
    Task OnMessageAsync(IMemoryOwner<Message> message, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task OnCancelAsync(BlockRequest cancel, CancellationToken cancellationToken = default);
    Task OnDelayEnd(IPieceDelayer delayer, CancellationToken cancellationToken = default);
}
