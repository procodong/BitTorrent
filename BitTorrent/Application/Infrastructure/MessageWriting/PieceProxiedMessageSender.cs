using BitTorrentClient.Models.Messages;
using BitTorrentClient.Protocol.Networking.PeerWire;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Infrastructure.MessageWriting;
internal class PieceProxiedMessageSender : PipedMessageSender
{
    private readonly ChannelWriter<BlockData> _blockSender;
    public PieceProxiedMessageSender(PipeWriter pipe, ChannelWriter<BlockData> blockSender) : base(pipe)
    {
        _blockSender = blockSender;
    }

    public override async Task SendBlockAsync(BlockData block, CancellationToken cancellationToken = default)
    {
        await _blockSender.WriteAsync(block, cancellationToken);
    }
}
