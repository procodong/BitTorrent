using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Networking.PeerWire.Sending;

namespace BitTorrentClient.Application.Infrastructure.MessageWriting;
internal class MessageSenderProxy : IMessageSender
{
    private readonly ChannelWriter<ReadOnlyMemory<Message>> _messageSender;
    private readonly ChannelWriter<BlockData> _blockSender;
    private readonly PooledList<Message> _buffer;
    private readonly List<BlockData> _blocks;
    public MessageSenderProxy(ChannelWriter<ReadOnlyMemory<Message>> messageSender, ChannelWriter<BlockData> blockSender)
    {
        _messageSender = messageSender;
        _buffer = new();
        _blockSender = blockSender;
    }

    public void SendRelation(RelationUpdate relation)
    {
        _buffer.Add(new(relation));
    }

    public void SendHave(int piece)
    {
        _buffer.Add(new(piece));
    }

    public void SendRequest(PieceRequest request)
    {
        _buffer.Add(new(request, RequestType.Request));
    }

    public void SendCancel(PieceRequest cancel)
    {
        _buffer.Add(new(cancel, RequestType.Cancel));
    }

    public Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default)
    {
        _blocks.Add(block);
        return Task.CompletedTask;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_buffer.Length != 0)
        {
            await _messageSender.WriteAsync(_buffer.Take(), cancellationToken);
        }

        foreach (var block in _blocks)
        {
            await _blockSender.WriteAsync(block, cancellationToken);
        }
    }
}
