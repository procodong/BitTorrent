using BitTorrentClient.Application.Events.Handling.MessageWriting;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.MessageWriting;
public class MessageWriter : IMessageWriter
{
    private readonly IMessageSender _sender;
    private readonly List<BlockData> _queuedBlocks;
    private readonly PeerState _state;
    private readonly TransferTracker _tracker;

    public MessageWriter(IMessageSender sender, PeerState state)
    {
        _sender = sender;
        _queuedBlocks = [];
        _state = state;
        _tracker = new();
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _sender.FlushAsync(cancellationToken);
    }

    public void RemoveQueuedBlock(PieceRequest request)
    {
        _queuedBlocks.RemoveAll(v => v.Request == request);
    }

    public async Task WriteMessageAsync(Message message, IPieceDelayer delayer, CancellationToken cancellationToken = default)
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
                await SendBlockAsync(block, delayer, cancellationToken);
                break;
            case MessageType.Cancel:
                _sender.SendCancel(message.Data.Request);
                break;
        }
    }

    public async Task WriteQueuedBlock(IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        await SendBlockAsync(_queuedBlocks.Pop(), delayer, cancellationToken);
    }

    private async Task SendBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        int delay = _tracker.TimeUntilTransferRate(_state.TransferLimit.Uploaded);
        if (_tracker.TimeUntilTransferRate(_state.TransferLimit.Uploaded) < 0)
        {
            delayer.DelayNextPiece(delay);
            _queuedBlocks.Add(block);
        }
        else
        {
            await _sender.SendBlockAsync(block, cancellationToken);
            _state.DataTransfer.AtomicAddUpload(block.Request.Length);
            _tracker.RegisterTransfer(block.Request.Length);
        }
    }
}
