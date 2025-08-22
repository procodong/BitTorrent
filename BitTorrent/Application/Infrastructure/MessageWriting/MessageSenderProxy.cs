using BitTorrentClient.Models.Messages;
using System.Threading.Channels;
using BitTorrentClient.Helpers.DataStructures;
using System.Buffers;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Application.Infrastructure.MessageWriting;
internal class MessageSenderProxy : IMessageSender
{
    private readonly ChannelWriter<IMemoryOwner<Message>> _messageWriter;
    private readonly ChannelWriter<BlockRequest> _cancellationWriter;
    private readonly PooledList<Message> _buffer;
    private readonly List<BlockRequest> _queuedUploadCancels;
    public MessageSenderProxy(ChannelWriter<IMemoryOwner<Message>> messageSender, ChannelWriter<BlockRequest> cancellationWriter)
    {
        _messageWriter = messageSender;
        _cancellationWriter = cancellationWriter;
        _buffer = new();
        _queuedUploadCancels = [];
    }

    public void SendRelation(RelationUpdate relation)
    {
        _buffer.Add(new(relation));
    }

    public void SendHave(int piece)
    {
        _buffer.Add(new(piece));
    }

    public void SendRequest(BlockRequest request)
    {
        _buffer.Add(new(request, RequestType.Request));
    }

    public void SendCancel(BlockRequest cancel)
    {
        _buffer.Add(new(cancel, RequestType.Cancel));
    }

    public Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default)
    {
        _buffer.Add(new(block));
        return Task.CompletedTask;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_buffer.Length != 0)
        {
            await _messageWriter.WriteAsync(_buffer.Take(), cancellationToken);
        }

        foreach (var cancel in _queuedUploadCancels)
        {
            await _cancellationWriter.WriteAsync(cancel, cancellationToken);
        }
    }

    public void CancelUpload(BlockRequest cancel)
    {
        _queuedUploadCancels.Add(cancel);
    }
}
