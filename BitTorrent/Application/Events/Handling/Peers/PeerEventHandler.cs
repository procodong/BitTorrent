using BitTorrentClient.Application.Events.Listening.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.Peers;
internal class PeerEventHandler : IPeerEventHandler
{
    private readonly IPeer _peer;

    public PeerEventHandler(IPeer peer)
    {
        _peer = peer;
    }

    public async Task OnBitfieldAsync(Stream bitfield, CancellationToken cancellationToken = default)
    {
        if (bitfield.Length != _peer.DownloadedPieces.Size)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPacketSize);
        }
        var buffer = new byte[bitfield.Length];
        await bitfield.ReadExactlyAsync(buffer, cancellationToken);
        _peer.DownloadedPieces = new(new(buffer));
    }

    public async Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        await _peer.CancelUploadAsync(request);
    }

    public Task OnClientHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        _peer.NotifyHavePiece(piece);
        return Task.CompletedTask;
    }

    public Task OnClientRelationAsync(PeerRelation relation, CancellationToken cancellationToken = default)
    {
        _peer.WantsToDownload = relation.Interested;
        _peer.Uploading = !relation.Choked;
        return Task.CompletedTask;
    }

    public Task OnPeerHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        _peer.DownloadedPieces[piece] = true;
        return Task.CompletedTask;
    }

    public Task OnPeerRelationAsync(RelationUpdate relation, CancellationToken cancellationToken = default)
    {
        switch (relation)
        {
            case RelationUpdate.Choke:
                _peer.Downloading = false;
                break;
            case RelationUpdate.Unchoke:
                _peer.Downloading = true;
                break;
            case RelationUpdate.Interested:
                _peer.WantsToUpload = true;
                break;
            case RelationUpdate.NotInterested:
                _peer.WantsToUpload = false;
                break;
        }
        return Task.CompletedTask;
    }

    public async Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default)
    {
        await _peer.RequestDownloadAsync(piece, cancellationToken);
    }

    public async Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        await _peer.RequestUploadAsync(request, cancellationToken);
    }
}
