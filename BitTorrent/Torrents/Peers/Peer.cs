using BitTorrent.Errors;
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
    private readonly List<QueuedPieceRequest> _pieceDownloads = [];
    private readonly Queue<PieceRequest> _requestQueue = [];
    private bool _writing = false;

    public Peer(PeerWireStream connection, ChannelReader<int> haveMessages, ChannelReader<PeerRelation> relationReceiver, Download download, SharedPeerState stats)
    {
        _connection = connection;
        _download = download;
        _state = stats;
        _haveMessageReceiver = haveMessages;
        _relationReceiver = relationReceiver;
    }

    public async Task ListenAsync()
    {
        Task receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength);
        Task<PeerRelation> relationTask = _relationReceiver.ReadAsync().AsTask();
        Task<int> haveTask = _haveMessageReceiver.ReadAsync().AsTask();
        Task keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval);
        Task completionTask = _relationReceiver.Completion;
        while (true)
        {
            var readyTask = await Task.WhenAny(receiveTask, relationTask, haveTask, keepAliveTask, completionTask);
            if (_writing || readyTask == receiveTask)
            {
                await receiveTask;
                receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength);
            }
            if (readyTask == relationTask)
            {
                PeerRelation relation = await relationTask;
                UpdateRelation(relation);
                relationTask = _relationReceiver.ReadAsync().AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = await haveTask;
                _connection.WriteHaveMessage(have);
                haveTask = _haveMessageReceiver.ReadAsync().AsTask();
            }
            else if (readyTask == keepAliveTask)
            {
                await keepAliveTask;
                _connection.WriteKeepAlive();
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval);
            }
            else if (readyTask == completionTask)
            {
                _state.Completion.SetCanceled();
                break;
            }
            await DequeRequests();
            if (_connection.Written)
            {
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval);
            }
            RequestBlocks();
            await _connection.FlushAsync();
        }
    }

    private QueuedPieceRequest FindDownload(PieceRequest request)
    {
        var piece = _pieceDownloads.Find(d => d.Request == request);
        if (piece.Download is null)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
        return piece;
    }

    private async Task DequeRequests()
    {
        while (_requestQueue.TryDequeue(out var request))
        {
            var block = _download.RequestBlock(request);
            if (block is null) return;
            await _connection.WritePieceAsync(new(request.Index, request.Begin), block);
        }
    }

    private void RequestBlocks()
    {
        if (_state.RelationToMe.Choked)
        {
            return;
        }
        while (_pieceDownloads.Count < _download.Config.RequestQueueSize)
        {
            QueuedPieceRequest? block;
            lock (_download)
            {
                block = _download.AssignBlockRequest(_state.OwnedPieces);
            }
            if (!block.HasValue)
            {
                break;
            }
            Console.WriteLine($"Requested!, {block.Value.Request.Index}, {block.Value.Request.Begin}");
            _pieceDownloads.Add(block.Value);
            _connection.WritePieceRequest(block.Value.Request);
        }
    }

    private void UpdateRelation(PeerRelation relation)
    {
        Console.WriteLine($"interested: {relation.Interested}, choked: {relation.Choked}");
        if (relation.Interested != _state.Relation.Interested)
        {
            _connection.WriteUpdateRelation(relation.Interested ? Relation.Interested : Relation.NotInterested);
        }
        if (relation.Choked != _state.Relation.Choked)
        {
            _connection.WriteUpdateRelation(relation.Choked ? Relation.Choke : Relation.Unchoke);
        }
    }

    public Task OnChokeAsync()
    {
        _state.RelationToMe = _state.RelationToMe with { Choked = true };
        return Task.CompletedTask;
    }

    public Task OnUnchokedAsync()
    {
        _state.RelationToMe = _state.RelationToMe with { Choked = false };
        return Task.CompletedTask;
    }

    public Task OnInterestedAsync()
    {
        _state.RelationToMe = _state.RelationToMe with { Interested = true };
        return Task.CompletedTask;
    }

    public Task OnNotInterestedAsync()
    {
        _state.RelationToMe = _state.RelationToMe with { Choked = false };
        return Task.CompletedTask;
    }

    public Task OnHaveAsync(int piece)
    {
        _state.OwnedPieces[piece] = true;
        return Task.CompletedTask;
    }

    public Task OnBitfieldAsync(BitArray bitfield)
    {
        _state.OwnedPieces = bitfield;
        return Task.CompletedTask;
    }

    public async Task OnRequestAsync(PieceRequest request)
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
            await _connection.WritePieceAsync(new(request.Index, request.Begin), block);
        }
        finally
        {
            _writing = false;
        }
        _download.FinishUpload(request.Length);
    }

    public async Task OnPieceAsync(Piece piece)
    {
        long blockLength = piece.Stream.Length - piece.Stream.Position;
        QueuedPieceRequest request = FindDownload(piece.Request);
        await _download.SaveBlockAsync(piece.Stream, request);
        Interlocked.Add(ref _state.Stats.Downloaded, blockLength);
    }

    public Task OnCancelAsync(PieceRequest request)
    {
        FindDownload(request);
        lock (_download)
        {
            _download.Cancel(request);
        }
        _pieceDownloads.RemoveAll(download => download.Request == request);
        return Task.CompletedTask;
    }

    public Task OnPortAsync(ushort port)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
