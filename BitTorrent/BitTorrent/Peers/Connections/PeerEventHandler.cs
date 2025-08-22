using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.BitTorrent.Downloads;
using BitTorrentClient.BitTorrent.Peers.Errors;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;
using BitTorrentClient.Helpers;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.BitTorrent.Peers.Connections;

public class PeerEventHandler : IAsyncDisposable, IPeerEventHandler
{
    private readonly PeerWireReader _connection;
    private readonly IPeer _peer;

    public PeerEventHandler(PeerWireReader connection, IPeer peer)
    {
        _connection = connection;
        _peer = peer;
    }

    public async Task ListenAsync(ChannelReader<int> haveMessageReader, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        Task receiveTask = _connection.ReceiveAsync(this, cancellationToken);
        Task<PeerRelation> relationTask = relationReader.ReadAsync(cancellationToken).AsTask();
        Task<int> haveTask = haveMessageReader.ReadAsync(cancellationToken).AsTask();
        while (true)
        {
            var readyTask = await Task.WhenAny(receiveTask, relationTask, haveTask);
            if (readyTask == receiveTask)
            {
                await receiveTask;
                receiveTask = _connection.ReceiveAsync(this, cancellationToken);
            }
            else if (readyTask == relationTask)
            {
                PeerRelation relation = await relationTask;
                _peer.Uploading = !relation.Choked;
                _peer.WantsToDownload = relation.Interested;
                relationTask = relationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = await haveTask;
                _peer.NotifyHavePiece(have);
                haveTask = haveMessageReader.ReadAsync(cancellationToken).AsTask();
            }

            await _peer.UpdateAsync(cancellationToken);
        }
    }

    public Task OnChokeAsync(CancellationToken cancellationToken = default)
    {
        _peer.Downloading = false;
        return Task.CompletedTask;
    }

    public Task OnUnChokedAsync(CancellationToken cancellationToken = default)
    {
        _peer.Downloading = true;
        return Task.CompletedTask;
    }

    public Task OnInterestedAsync(CancellationToken cancellationToken = default)
    {
        _peer.WantsToUpload = true;
        return Task.CompletedTask;
    }

    public Task OnNotInterestedAsync(CancellationToken cancellationToken = default)
    {
        _peer.WantsToUpload = false;
        return Task.CompletedTask;
    }

    public Task OnHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        _peer.BitArray[piece] = true;
        return Task.CompletedTask;
    }

    public Task OnBitfieldAsync(ZeroCopyBitArray bitfield, CancellationToken cancellationToken = default)
    {
        _peer.BitArray = new(bitfield);
        return Task.CompletedTask;
    }

    public Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        return _peer.UploadAsync(request, cancellationToken);
    }

    public Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default)
    {
        return _peer.DownloadAsync(piece, cancellationToken);
    }

    public Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        return _peer.CancelUploadAsync(request);
    }

    public ValueTask DisposeAsync()
    {
        return _peer.DisposeAsync();
    }
}
