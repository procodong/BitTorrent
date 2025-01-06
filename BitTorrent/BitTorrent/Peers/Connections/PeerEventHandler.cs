using BitTorrentClient.Models.Messages;
using BitTorrentClient.Models.Peers;
using BitTorrentClient.BitTorrent.Downloads;
using BitTorrentClient.BitTorrent.Peers.Errors;
using BitTorrentClient.BitTorrent.Peers.Streaming;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;

namespace BitTorrentClient.BitTorrent.Peers.Connections;

public class PeerEventHandler : IDisposable, IAsyncDisposable, IPeerEventHandler
{
    private readonly PeerWireStream _connection;
    private readonly Download _download;
    private readonly PeerState _state;
    private readonly List<Block> _pieceDownloads = [];
    private readonly Queue<PieceRequest> _requestQueue = [];
    private BlockCursor? _blockCursor;
    private bool _writing;

    public PeerEventHandler(PeerWireStream connection, Download download, PeerState state)
    {
        _connection = connection;
        _download = download;
        _state = state;
    }

    public async Task ListenAsync(ChannelReader<int> haveMessageReader, ChannelReader<PeerRelation> relationReader, CancellationToken cancellationToken = default)
    {
        Task receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength, cancellationToken);
        Task<PeerRelation> relationTask = relationReader.ReadAsync(cancellationToken).AsTask();
        Task<int> haveTask = haveMessageReader.ReadAsync(cancellationToken).AsTask();
        Task keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval, cancellationToken);
        while (true)
        {
            var readyTask = await Task.WhenAny(receiveTask, relationTask, haveTask, keepAliveTask);
            if (_writing || readyTask == receiveTask)
            {
                await receiveTask;
                receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength, cancellationToken);
            }
            if (readyTask == relationTask)
            {
                PeerRelation relation = await relationTask;
                UpdateRelation(relation);
                relationTask = relationReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = await haveTask;
                _connection.WriteHaveMessage(have);
                haveTask = haveMessageReader.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == keepAliveTask)
            {
                await keepAliveTask;
                _connection.WriteKeepAlive();
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval, cancellationToken);
            }
            await DequeRequests(cancellationToken);
            if (!_state.RelationToMe.Choked && _state.Relation.Interested)
            {
                RequestBlocks();
            }
            if (_connection.Written)
            {
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval, cancellationToken);
            }
            await _connection.FlushAsync(cancellationToken);
        }
    }

    private Block FindDownload(PieceRequest request)
    {
        var piece = _pieceDownloads.Find(block => block == request);
        if (piece.Piece is null)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
        return piece;
    }

    private async Task DequeRequests(CancellationToken cancellationToken = default)
    {
        while (_requestQueue.TryDequeue(out var request))
        {
            var block = _download.RequestBlock(request);
            if (block is null) return;
            await _connection.WritePieceAsync(new(request.Index, request.Begin), block, cancellationToken);
            FinishUpload(request.Length);
        }
    }

    private void FinishUpload(int size)
    {
        _download.FinishUpload(size);
        Interlocked.Add(ref _state.DataTransfer.Uploaded, size);
    }

    private void RequestBlocks()
    {
        while (_pieceDownloads.Count < _download.Config.RequestQueueSize)
        {
            Block request = _blockCursor?.GetRequest(_download.Config.RequestSize) ?? new();
            while (request.Length == 0)
            {
                Block? block;
                lock (_download)
                {
                    block = _download.AssignBlock(_state.OwnedPieces);
                }
                if (block is not null)
                {
                    _blockCursor = new BlockCursor(block.Value);
                }
                else
                {
                    _blockCursor = null;
                    return;
                }
                request = _blockCursor.GetRequest(_download.Config.RequestSize);
            }
            _pieceDownloads.Add(request);
            _connection.WritePieceRequest(request);
        }
    }

    private void UpdateRelation(PeerRelation relation)
    {
        if (relation.Interested != _state.Relation.Interested)
        {
            _connection.WriteUpdateRelation(relation.Interested ? Relation.Interested : Relation.NotInterested);
        }
        if (relation.Choked != _state.Relation.Choked)
        {
            _connection.WriteUpdateRelation(relation.Choked ? Relation.Choke : Relation.Unchoke);
        }
    }

    public Task OnChokeAsync(CancellationToken cancellationToken = default)
    {
        _state.RelationToMe = _state.RelationToMe with { Choked = true };
        CancelAll();
        return Task.CompletedTask;
    }

    public Task OnUnchokedAsync(CancellationToken cancellationToken = default)
    {
        _state.RelationToMe = _state.RelationToMe with { Choked = false };
        return Task.CompletedTask;
    }

    public Task OnInterestedAsync(CancellationToken cancellationToken = default)
    {
        _state.RelationToMe = _state.RelationToMe with { Interested = true };
        return Task.CompletedTask;
    }

    public Task OnNotInterestedAsync(CancellationToken cancellationToken = default)
    {
        _state.RelationToMe = _state.RelationToMe with { Choked = false };
        return Task.CompletedTask;
    }

    public Task OnHaveAsync(int piece, CancellationToken cancellationToken = default)
    {
        _state.OwnedPieces[piece] = true;
        return Task.CompletedTask;
    }

    public Task OnBitfieldAsync(BitArray bitfield, CancellationToken cancellationToken = default)
    {
        _state.OwnedPieces = bitfield;
        return Task.CompletedTask;
    }

    public async Task OnRequestAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        if (_state.Relation.Choked || !_download.DownloadedPieces[request.Index]) return;
        if (request.Length > _download.Config.MaxRequestSize)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
        var block = _download.RequestBlock(request);
        if (block is null)
        {
            _requestQueue.Enqueue(request);
            return;
        }
        _writing = true;
        try
        {
            await _connection.WritePieceAsync(new(request.Index, request.Begin), block, cancellationToken);
        }
        finally
        {
            _writing = false;
        }
        FinishUpload(request.Length);
    }

    public async Task OnPieceAsync(BlockData piece, CancellationToken cancellationToken = default)
    {
        Block block = FindDownload(piece.Request);
        _pieceDownloads.Remove(block);
        await _download.SaveBlockAsync(piece.Stream, block, cancellationToken);
        Interlocked.Add(ref _state.DataTransfer.Downloaded, piece.Request.Length);
    }

    public Task OnCancelAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        var download = FindDownload(request);
        lock (_download)
        {
            _download.Cancel(download);
        }
        _pieceDownloads.Remove(download);
        return Task.CompletedTask;
    }

    private void CancelAll()
    {
        Block? canceledRequest = default;
        foreach (var download in _pieceDownloads)
        {
            if (canceledRequest is null)
            {
                canceledRequest = download;
            }
            else if (canceledRequest.Value.Piece == download.Piece && canceledRequest.Value.Begin + canceledRequest.Value.Length == download.Begin)
            {
                canceledRequest = canceledRequest.Value with
                {
                    Length = canceledRequest.Value.Length + download.Length
                };
            }
            else
            {
                _download.Cancel(canceledRequest.Value);
                canceledRequest = download;
            }
        }
        _pieceDownloads.Clear();
        if (_blockCursor is not null)
        {
            var remaining = _blockCursor.GetRequest(_blockCursor.Remaining);
            _download.Cancel(remaining);
        }
    }

    public void Dispose()
    {
        lock (_download)
        {
            CancelAll();
        }
        _connection.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        lock (_download)
        {
            CancelAll();
        }
        return _connection.DisposeAsync();
    }
}
