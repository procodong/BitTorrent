using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Application.Infrastructure.MessageWriting.Interface;

namespace BitTorrentClient.Application.Infrastructure.MessageWriting;
internal class MessageWriter : IMessageWriter
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

    public void WriteRelation(RelationUpdate relation)
    {
        _sender.SendRelation(relation);
    }

    public void WriteHave(int piece)
    {
        _sender.SendHave(piece);
    }

    public void WriteRequest(BlockRequest request)
    {
        _sender.SendRequest(request);
    }

    public void WriteCancel(BlockRequest cancel)
    {
        _sender.SendCancel(cancel);
    }

    public async Task WriteBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        int delay = _tracker.TimeUntilTransferRate(_state.TransferLimit.Uploaded);
        if (_tracker.TimeUntilTransferRate(_state.TransferLimit.Uploaded) < 0)
        {
            delayer.DelayNextPiece(-delay);
            _queuedBlocks.Add(block);
        }
        else
        {
            await _sender.SendBlockAsync(block, cancellationToken);
            _state.DataTransfer.AtomicAddUpload(block.Request.Length);
            _tracker.RegisterTransfer(block.Request.Length);
        }
    }

    public Task WriteBlockAsync(IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        if (_queuedBlocks.Count != 0)
        {
            return WriteBlockAsync(_queuedBlocks.Pop(), delayer, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public bool TryCancelUpload(BlockRequest request)
    {
        int index = _queuedBlocks.FindIndex(b => b.Request == request);
        if (index == -1) return false;
        _queuedBlocks.SwapRemove(index);
        return true;
    }
}
