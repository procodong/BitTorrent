using BitTorrentClient.Models.Messages;
using BitTorrentClient.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire;
using BitTorrentClient.Application.EventHandling.Peers;
using BitTorrentClient.Protocol.Networking.PeerWire;
using BitTorrentClient.Protocol.Networking.PeerWire.Sending;

namespace BitTorrentClient.Application.Infrastructure.Peers;
public class Peer : IPeer, IDisposable
{
    private readonly PeerState _state;
    private readonly ChannelWriter<PieceRequest> _cancellationWriter;
    private readonly IMessageSender _sender;
    private readonly IBlockRequester _requester;

    public Peer(PeerState state, IBlockRequester requester, IMessageSender sender, ChannelWriter<PieceRequest> cancellationWriter)
    {
        _state = state;
        _sender = sender;
        _cancellationWriter = cancellationWriter;
        _requester = requester;
    }

    public bool Downloading
    {
        get => !_state.RelationToMe.Choked;
        set
        {
            if (!value)
            {
                CancelAll();
            }
            _state.RelationToMe = _state.RelationToMe with { Choked = !value };
        }
    }
    public bool WantsToDownload
    {
        get => _state.Relation.Interested;
        set
        {
            if (_state.Relation.Interested == value) return;
            _state.Relation = _state.Relation with { Interested = value };
            _sender.SendRelation(value ? RelationUpdate.Interested : RelationUpdate.NotInterested);
        }
    }
    public bool Uploading
    {

        get => !_state.Relation.Choked;
        set
        {
            if (_state.Relation.Choked == !value) return;
            _state.Relation = _state.Relation with { Choked = !value };
            _sender.SendRelation(value ? RelationUpdate.Unchoke : RelationUpdate.Choke);
        }
    }
    public bool WantsToUpload { get => _state.RelationToMe.Interested; set => _state.RelationToMe = _state.RelationToMe with { Interested = value }; }
    public LazyBitArray DownloadedPieces { get => _state.OwnedPieces; set => _state.OwnedPieces = value; }

    public async Task CancelUploadAsync(PieceRequest request)
    {
        await _cancellationWriter.WriteAsync(request);
    }

    public async Task RequestUploadAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uploading) return;
        if (_requester.TryGetBlock(request, out var stream))
        {
            await _sender.SendBlockAsync(new(request, stream), cancellationToken);
        }
    }

    public async Task RequestDownloadAsync(BlockData blockData, CancellationToken cancellationToken = default)
    {
        await _requester.SaveBlockAsync(blockData, cancellationToken);
        _state.DataTransfer.AtomicAddDownload(blockData.Request.Length);
    }

    public void NotifyHavePiece(int piece)
    {
        _sender.SendHave(piece);
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        while (_requester.TryRequestDownload(DownloadedPieces, out var block))
        {
            _sender.SendRequest(block);
        }
        await _sender.FlushAsync(cancellationToken);
    }

    private void CancelAll()
    {
        foreach (var request in _requester.DrainRequests())
        {
            _sender.SendCancel(request);
        }
    }

    public void Dispose()
    {
        CancelAll();
    }
}
