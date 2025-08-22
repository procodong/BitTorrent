using BitTorrentClient.Application.Events.Handling.MessageWriting;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.MessageWriting;
public class MessageWriter : IMessageWriter
{
    private readonly IMessageSender _sender;
    private readonly List<BlockData> _queuedBlocks;
    private readonly PeerState _state;

    public MessageWriter(IMessageSender sender, PeerState state)
    {
        _sender = sender;
        _queuedBlocks = [];
        _state = state;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _sender.FlushAsync(cancellationToken);
    }

    public void RemoveQueuedBlock(PieceRequest request)
    {
        _queuedBlocks.RemoveAll(v => v.Request == request);
    }

    public async Task WriteMessageAsync(Message message, IPieceDelayer pieceDelayer, CancellationToken cancellationToken = default)
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
                var header = message.Data.Piece;
                var req = new PieceRequest(header.Index, header.Begin, (int)message.Body.Length);
                var block = new BlockData(req, message.Body);
                await _sender.SendBlockAsync(block, cancellationToken);
                _state.DataTransfer.AtomicAddUpload(req.Length);
                break;
            case MessageType.Cancel:
                _sender.SendCancel(message.Data.Request);
                break;
        }
    }

    public async Task WriteQueuedBlock(CancellationToken cancellationToken = default)
    {
        await _sender.SendBlockAsync(_queuedBlocks.Pop(), cancellationToken);
    }
}
