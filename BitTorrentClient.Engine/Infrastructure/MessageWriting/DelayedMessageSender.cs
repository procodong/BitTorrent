using BitTorrentClient.Engine.Infrastructure.MessageWriting.Interface;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Engine.Infrastructure.MessageWriting;
public sealed class DelayedMessageSender : IDelayedMessageSender
{
    private readonly IPeerWireWriter _sender;
    private readonly List<BlockData> _queuedBlocks;
    private readonly PeerState _state;
    private readonly TransferTracker _tracker;

    public DelayedMessageSender(IPeerWireWriter sender, PeerState state)
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

    public void SendRelation(RelationUpdate relation)
    {
        _sender.SendRelation(relation);
    }

    public void SendHave(int piece)
    {
        _sender.SendHave(piece);
    }

    public void SendRequest(BlockRequest request)
    {
        _sender.SendRequest(request);
    }

    public void SendCancel(BlockRequest cancel)
    {
        _sender.SendCancel(cancel);
    }

    public void SendKeepAlive()
    {
        _sender.SendKeepAlive();
    }

    public async Task SendBlockAsync(BlockData block, IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        var delay = _tracker.TimeUntilTransferRate(_state.TransferLimit.Uploaded);
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

    public Task SendBlockAsync(IPieceDelayer delayer, CancellationToken cancellationToken = default)
    {
        if (_queuedBlocks.Count != 0)
        {
            return SendBlockAsync(_queuedBlocks.Pop(), delayer, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public bool TryCancelUpload(BlockRequest request)
    {
        var index = _queuedBlocks.FindIndex(b => b.Request == request);
        if (index == -1) return false;
        _queuedBlocks.SwapRemove(index);
        return true;
    }
}
