using System.Buffers;
using System.Runtime.InteropServices;
using BitTorrentClient.Application.Events.Listening.MessageWriting;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Application.Events.Handling.MessageWriting;

public class MessageWritingEventHandler : IMessageWritingEventHandler
{
    private readonly IMessageWriter _writer;
    
    public MessageWritingEventHandler(IMessageWriter sender)
    {
        _writer = sender;
    }

    public async Task OnMessageAsync(IMemoryOwner<Message> messages, IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        using var _ = messages;
        var iter = MemoryMarshal.ToEnumerable<Message>(messages.Memory);
        foreach (var message in iter)
        {
            await _writer.WriteMessageAsync(message, delayer, cancellationToken);
        }
        await _writer.FlushAsync(cancellationToken);
    }

    public Task OnCancelAsync(PieceRequest cancel, CancellationToken cancellationToken = default)
    {
        _writer.RemoveQueuedBlock(cancel);
        return Task.CompletedTask;
    }

    public async Task OnDelayEnd(CancellationToken cancellationToken = default)
    {
        await _writer.WriteQueuedBlock(cancellationToken);
    }
}