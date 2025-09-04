using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;
using BitTorrentClient.Engine.Models.Messages;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Events.Handling;

public class MessageWritingEventHandler : IMessageWritingEventHandler
{
    private readonly IDelayedMessageSender _sender;
    
    public MessageWritingEventHandler(IDelayedMessageSender sender)
    {
        _sender = sender;
    }

    public async Task OnMessageAsync(MaybeRentedArray<Message> messages, IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        using var _ = messages;
        foreach (var message in messages.Data)
        {
            switch (message.Type)
            {
                case MessageType.Choke:
                case MessageType.UnChoke:
                case MessageType.Interested:
                case MessageType.NotInterested:
                    _sender.SendRelation((RelationUpdate)message.Type);
                    break;
                case MessageType.Have:
                    _sender.SendHave(message.Data.Have);
                    break;
                case MessageType.Request:
                    _sender.SendRequest(message.Data.Request);
                    break;
                case MessageType.Piece:
                    var header = message.Data.Block;
                    var req = new BlockRequest(header.Index, header.Begin, (int)message.Body.Length);
                    var block = new BlockData(req, message.Body);
                    await _sender.SendBlockAsync(block, delayer, cancellationToken);
                    break;
                case MessageType.Cancel:
                    _sender.SendCancel(message.Data.Request);
                    break;
            }
        }
        await _sender.FlushAsync(cancellationToken);
    }

    public Task OnCancelAsync(BlockRequest cancel, CancellationToken cancellationToken = default)
    {
        _sender.TryCancelUpload(cancel);
        return Task.CompletedTask;
    }

    public async Task OnDelayEnd(IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        await _sender.SendBlockAsync(delayer, cancellationToken);
    }

    public Task OnKeepAliveAsync(CancellationToken cancellationToken = default)
    {
        _sender.SendKeepAlive();
        return Task.CompletedTask;
    }
}