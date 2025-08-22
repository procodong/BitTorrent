using BitTorrentClient.Models.Messages;
using System.Buffers;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Application.Infrastructure.MessageWriting.Interface;

namespace BitTorrentClient.Application.Events.Listening.MessageWriting;
internal interface IMessageWritingEventHandler
{
    Task OnMessageAsync(IMemoryOwner<Message> message, IPieceDelayer delayer, CancellationToken cancellationToken = default);
    Task OnCancelAsync(BlockRequest cancel, CancellationToken cancellationToken = default);
    Task OnDelayEnd(IPieceDelayer delayer, CancellationToken cancellationToken = default);
}
