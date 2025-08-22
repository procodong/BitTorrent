using System.Buffers;
using System.Runtime.CompilerServices;
using BitTorrentClient.Application.EventListening.MessageWriting;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Application.EventHandling.MessageWriting;

public class MessageWritingEventHandler : IMessageWritingEventHandler
{
    public async Task OnMessageAsync(ReadOnlyMemory<Message> message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task OnBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task OnCancelAsync(PieceRequest cancel, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}