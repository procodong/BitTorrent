using System.Buffers;
using System.Runtime.InteropServices;
using BitTorrentClient.Application.Events.Listening.MessageWriting;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Application.Events.Handling;

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
            switch (message.Type)
            {
                case MessageType.Choke:
                case MessageType.UnChoke:
                case MessageType.Interested:
                case MessageType.NotInterested:
                    _writer.WriteRelation((RelationUpdate)message.Type);
                    break;
                case MessageType.Have:
                    _writer.WriteHave(message.Data.Have);
                    break;
                case MessageType.Request:
                    _writer.WriteRequest(message.Data.Request);
                    break;
                case MessageType.Piece:
                    var header = message.Data.Block;
                    var req = new BlockRequest(header.Index, header.Begin, (int)message.Body.Length);
                    var block = new BlockData(req, message.Body);
                    await _writer.WriteBlockAsync(block, delayer, cancellationToken);
                    break;
                case MessageType.Cancel:
                    _writer.WriteCancel(message.Data.Request);
                    break;
            }
        }
        await _writer.FlushAsync(cancellationToken);
    }

    public Task OnCancelAsync(BlockRequest cancel, CancellationToken cancellationToken = default)
    {
        _writer.TryCancelUpload(cancel);
        return Task.CompletedTask;
    }

    public async Task OnDelayEnd(IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        await _writer.WriteBlockAsync(delayer, cancellationToken);
    }
}