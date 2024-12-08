using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.Peers.Errors;
using System.Collections;
using System.Net.Sockets;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Peers;

public class Peer : IDisposable, IAsyncDisposable, IPeerEventHandler
{
    private readonly PeerWireStream _connection;
    private readonly Download _download;
    private readonly SharedPeerState _state;
    private readonly ChannelReader<int> _haveMessageReceiver;
    private readonly ChannelReader<PeerRelation> _relationReceiver;
    private readonly List<Block> _pieceDownloads = [];
    private PieceSegmentHandle? _segment;
    private readonly Queue<PieceRequest> _requestQueue = [];
    private bool _writing = false;

    public Peer(PeerWireStream connection, ChannelReader<int> haveMessages, ChannelReader<PeerRelation> relationReceiver, Download download, SharedPeerState stats)
    {
        _connection = connection;
        _download = download;
        _state = stats;
        _haveMessageReceiver = haveMessages;
        _relationReceiver = relationReceiver;
        lock (download)
        {
            _segment = download.AssignSegment(stats.OwnedPieces);
        }
    }

    public async Task ListenAsync(CancellationToken cancellationToken = default)
    {
        Task receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength, cancellationToken);
        Task<PeerRelation> relationTask = _relationReceiver.ReadAsync(cancellationToken).AsTask();
        Task<int> haveTask = _haveMessageReceiver.ReadAsync(cancellationToken).AsTask();
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
                relationTask = _relationReceiver.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = await haveTask;
                _connection.WriteHaveMessage(have);
                haveTask = _haveMessageReceiver.ReadAsync(cancellationToken).AsTask();
            }
            else if (readyTask == keepAliveTask)
            {
                await keepAliveTask;
                _connection.WriteKeepAlive();
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval, cancellationToken);
            }
            await DequeRequests(cancellationToken);
            RequestBlocks();
            if (_connection.Written)
            {
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval, cancellationToken);
            }
            await _connection.FlushAsync();
        }
    }

    private Block FindDownload(PieceRequest request)
    {
        var piece = _pieceDownloads.Find(d => d.Begin == request.Begin && d.Piece.PieceIndex == request.Index && d.Length == request.Length);
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
        Interlocked.Add(ref _state.Stats.Uploaded, size);
    }

    private void RequestBlocks()
    {
        if (_state.RelationToMe.Choked || _segment is null)
        {
            return;
        }
        while (_pieceDownloads.Count < _download.Config.RequestQueueSize)
        {
            PieceRequest block = _segment.GetRequest(_download.Config.RequestSize);
            if (block.Length == 0)
            {
                PieceSegmentHandle? segment;
                lock (_download)
                {
                    segment = _download.AssignSegment(_state.OwnedPieces);
                }
                if (segment is not null)
                {
                    _segment = segment;
                }
                block = _segment.GetRequest(_download.Config.RequestSize);
            }
            if (block.Length != 0)
            {
                _pieceDownloads.Add(new(_segment.Piece, block.Begin, block.Length));
                _connection.WritePieceRequest(block);
            }
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

    public async Task OnPieceAsync(Piece piece, CancellationToken cancellationToken = default)
    {
        Block block = FindDownload(piece.Request);
        await _download.SaveBlockAsync(piece.Stream, block, cancellationToken);
        Interlocked.Add(ref _state.Stats.Downloaded, piece.Request.Length);
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

    public Task OnPortAsync(ushort port, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private void CancelAll()
    {
        if (_segment is not null)
        {
            _download.Cancel(new(_segment.Piece, _segment.Position, _segment.Piece.Size - _segment.Position));
        }
        foreach (var download in _pieceDownloads)
        {
            _download.Cancel(download);
        }
    }

    public void Dispose()
    {
        CancelAll();
        _connection.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        CancelAll();
        return _connection.DisposeAsync();
    }
}
