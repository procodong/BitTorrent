using BitTorrent.Errors;
using BitTorrent.Models.Messages;
using BitTorrent.Models.Peers;
using BitTorrent.Torrents.Downloads;
using BitTorrent.Torrents.PeerManaging;
using BitTorrent.Torrents.Peers.Errors;
using System.Collections;
using System.Net.Sockets;
using System.Threading.Channels;

namespace BitTorrent.Torrents.Peers;

public class Peer : IDisposable, IAsyncDisposable, IPeerEventHandler
{
    private readonly PeerWireStream _connection;
    private readonly Download _download;
    private readonly SharedPeerData _stats;
    private readonly ChannelReader<int> _haveMessageReceiver;
    private readonly ChannelReader<PeerRelation> _relationReceiver;
    private readonly List<QueuedPieceRequest> _pieceDownloads = [];
    private bool _writing = false;

    public Peer(PeerWireStream connection, ChannelReader<int> haveMessages, ChannelReader<PeerRelation> relationReceiver, Download download, SharedPeerData stats)
    {
        _connection = connection;
        _download = download;
        _stats = stats;
        _haveMessageReceiver = haveMessages;
        _relationReceiver = relationReceiver;
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

    private void RequestBlocks()
    {
        if (_stats.RelationToMe.Choked || _download.FinishedDownloading)
        {
        }
        while (_pieceDownloads.Count < _download.Config.RequestQueueSize)
        {
            QueuedPieceRequest? block; 
            lock (_download)
            {
                block = _download.AssignBlockRequest(_stats.OwnedPieces);
            }
            if (!block.HasValue)
            {
                break;
            }
            _pieceDownloads.Add(block.Value);
            _connection.WritePieceRequest(block.Value.Request);
        }
    }

    private void UpdateRelation(PeerRelation relation)
    {
        if (relation.Interested != _stats.Relation.Interested)
        {
            _connection.WriteUpdateRelation(relation.Interested ? Relation.Interested : Relation.NotInterested);
        }
        if (relation.Choked != _stats.Relation.Choked)
        {
            _connection.WriteUpdateRelation(relation.Choked ? Relation.Choke : Relation.Unchoke);
        }
    }

    public async Task ListenAsync()
    {
        Task receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength);
        Task<PeerRelation> relationTask = _relationReceiver.ReadAsync().AsTask();
        Task<int> haveTask = _haveMessageReceiver.ReadAsync().AsTask();
        Task keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval);
        while (true)
        {
            var readyTask = await Task.WhenAny(receiveTask, relationTask, haveTask, keepAliveTask);
            if (_writing)
            {
                await receiveTask;
            }
            if (readyTask == receiveTask)
            {
                receiveTask = _connection.ReceiveAsync(this, _download.MaxMessageLength);
            }
            else if (readyTask == relationTask)
            {
                PeerRelation relation = relationTask.Result;
                UpdateRelation(relation);
                relationTask = _relationReceiver.ReadAsync().AsTask();
            }
            else if (readyTask == haveTask)
            {
                int have = haveTask.Result;
                _connection.WriteHaveMessage(have);
                haveTask = _haveMessageReceiver.ReadAsync().AsTask();
            }
            else if (readyTask == keepAliveTask)
            {
                _connection.WriteKeepAlive();
                keepAliveTask = Task.Delay(_download.Config.KeepAliveInterval);
            }
            await _connection.FlushAsync();
            if (_relationReceiver.Completion.IsCompleted) break;
        }
    }

    public Task OnChokeAsync()
    {
        _stats.RelationToMe = _stats.RelationToMe with { Choked = true };
        return Task.CompletedTask;
    }

    public Task OnUnchokedAsync()
    {
        _stats.RelationToMe = _stats.RelationToMe with { Choked = false };
        RequestBlocks();
        return Task.CompletedTask;
    }

    public Task OnInterestedAsync()
    {
        _stats.RelationToMe = _stats.RelationToMe with { Interested = true };
        return Task.CompletedTask;
    }

    public Task OnNotInterestedAsync()
    {
        _stats.RelationToMe = _stats.RelationToMe with { Choked = false };
        return Task.CompletedTask;
    }

    public Task OnHaveAsync(int piece)
    {
        _stats.OwnedPieces[piece] = true;
        return Task.CompletedTask;
    }

    public Task OnBitfieldAsync(BitArray bitfield)
    {
        _stats.OwnedPieces = bitfield;
        return Task.CompletedTask;
    }

    public async Task OnRequestAsync(PieceRequest request)
    {
        if (_stats.Relation.Choked) return;
        if (request.Length > _download.Config.MaxRequestSize)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
        var block = _download.RequestBlock(request);
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
        Interlocked.Add(ref _stats.Stats.Downloaded, blockLength);
        RequestBlocks();
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
