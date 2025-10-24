using BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;
using BitTorrentClient.Engine.Models.Messages;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Events.Handling.Interface;
public interface IMessageWritingEventHandler
{
    Task OnMessageAsync(MaybeRentedArray<Message> message, DelaySchedulingHandle delayer, CancellationToken cancellationToken = default);
    Task OnCancelAsync(BlockRequest cancel, CancellationToken cancellationToken = default);
    Task OnDelayEnd(DelaySchedulingHandle delayer, CancellationToken cancellationToken = default);
    Task OnKeepAliveAsync(CancellationToken cancellationToken = default);
}
