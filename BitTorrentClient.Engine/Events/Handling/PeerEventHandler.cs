using BitTorrentClient.Engine.Events.Handling.Interface;
using BitTorrentClient.Engine.Infrastructure.Peers.Interface;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Events.Handling;
public sealed class PeerEventHandler : IPeerEventHandler
{
    private readonly IPeer _peer;
    private readonly long _interestMin;

    public PeerEventHandler(IPeer peer, long interestMin)
    {
        _peer = peer;
        _interestMin = interestMin;
    }

    public Task OnBitfieldAsync(ZeroCopyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        _peer.DownloadedPieces = new(bitfield);
        return Task.CompletedTask;
    }

    public async Task OnCancelAsync(BlockRequest request, CancellationToken cancellationToken = default)
    {
        await _peer.CancelUploadAsync(request);
        await _peer.UpdateAsync(cancellationToken);
    }

    public async Task OnClientHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        _peer.NotifyHavePiece(piece);
        await _peer.UpdateAsync(cancellationToken);
    }

    public async Task OnClientRelationAsync(DataTransferVector transferLimit, CancellationToken cancellationToken = default)
    {
        _peer.Uploading = transferLimit.Upload >= _interestMin;
        _peer.WantsToDownload = transferLimit.Download >= _interestMin;
        _peer.TransferLimit = transferLimit;
        await _peer.UpdateAsync(cancellationToken);
    }

    public Task OnPeerHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        _peer.DownloadedPieces[piece] = true;
        return Task.CompletedTask;
    }

    public async Task OnPeerRelationAsync(RelationUpdate relation, CancellationToken cancellationToken = default)
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
        await _peer.UpdateAsync(cancellationToken);
    }

    public async Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default)
    {
        await _peer.RequestDownloadAsync(piece, cancellationToken);
        await _peer.UpdateAsync(cancellationToken);
    }

    public async Task OnRequestAsync(BlockRequest request, CancellationToken cancellationToken = default)
    {
        await _peer.RequestUploadAsync(request, cancellationToken);
        await _peer.UpdateAsync(cancellationToken);
    }
}
