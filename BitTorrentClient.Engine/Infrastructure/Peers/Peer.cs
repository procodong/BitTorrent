using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Engine.Infrastructure.Storage.Interface;
using BitTorrentClient.Engine.Models.Peers;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;
using BitTorrentClient.Protocol.Transport.PeerWire.Sending;

namespace BitTorrentClient.Engine.Infrastructure.Peers;
internal class Peer : IPeer, IDisposable
{
    private readonly PeerState _state;
    private readonly IMessageSender _sender;
    private readonly IBlockRequester _requester;

    public Peer(PeerState state, IBlockRequester requester, IMessageSender sender)
    {
        _state = state;
        _sender = sender;
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

    public Task CancelUploadAsync(BlockRequest request)
    {
        _sender.TryCancelUpload(request);
        return Task.CompletedTask;
    }

    public async Task RequestUploadAsync(BlockRequest request, CancellationToken cancellationToken = default)
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
