using BitTorrentClient.BitTorrent.Downloads;
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

namespace BitTorrentClient.BitTorrent.Peers.Connections;
public class Peer : IPeer
{
    private readonly Download _download;
    private readonly PeerState _state;
    private readonly List<Block> _blockDownloads;
    private readonly ChannelWriter<BlockData> _blockUploadWriter;
    private readonly ChannelWriter<PieceRequest> _cancellationWriter;
    private readonly PipeWriter _messagePipe;
    private BlockCursor? _blockCursor;

    public Peer(Download download, PeerState state, PipeWriter messagePipe, ChannelWriter<BlockData> blockUploader, ChannelWriter<PieceRequest> cancellationWriter)
    {
        _download = download;
        _state = state;
        _messagePipe = messagePipe;
        _blockUploadWriter = blockUploader;
        _cancellationWriter = cancellationWriter;
        _blockDownloads = [];
    }

    private MessageWriter Writer => new(_messagePipe);

    public bool Downloading { 
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
            Writer.WriteUpdateRelation(value ? Relation.Interested : Relation.NotInterested);
        }
    }
    public bool Uploading {
        
        get => !_state.Relation.Choked; 
        set
        {
            if (_state.Relation.Choked == !value) return;
            _state.Relation = _state.Relation with { Choked = !value };
            Writer.WriteUpdateRelation(value ? Relation.Unchoke : Relation.Choke);
        }
    }
    public bool WantsToUpload { get => _state.RelationToMe.Interested; set => _state.RelationToMe = _state.RelationToMe with { Interested = value }; }
    public LazyBitArray DownloadedPieces { get => _state.OwnedPieces; set => _state.OwnedPieces = value; }

    public async Task CancelUploadAsync(PieceRequest request)
    {
        await _cancellationWriter.WriteAsync(request);
    }

    public async Task UploadAsync(PieceRequest request, CancellationToken cancellationToken = default)
    {
        if (!_download.DownloadedPieces[request.Index] || !Uploading) return;
        var data = _download.RequestBlock(request);
        await _blockUploadWriter.WriteAsync(new(request, data), cancellationToken);
    }

    public async Task DownloadAsync(BlockData blockData, CancellationToken cancellationToken = default)
    {
        var requestIndex = _blockDownloads.FindIndex(req => req == blockData.Request);
        if (requestIndex == -1) return;
        var block = _blockDownloads[requestIndex];
        _blockDownloads.RemoveAt(requestIndex);
        await _download.SaveBlockAsync(blockData.Stream, block, cancellationToken);
        _state.DataTransfer.AtomicAddDownload(blockData.Request.Length);
    }
    
    public void NotifyHavePiece(int piece)
    {
        Writer.WriteHaveMessage(piece);
    }

    public async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        await _messagePipe.FlushAsync(cancellationToken);
        while (_blockDownloads.Count < 5)
        {
            if (!RequestBlock()) break;
        }
    }

    private bool RequestBlock()
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
                return false;
            }
            request = _blockCursor.GetRequest(_download.Config.RequestSize);
        }
        _blockDownloads.Add(request);
        Writer.WritePieceRequest(request);
        return true;
    }

    private void CancelAll()
    {
        Block? canceledRequest = default;
        foreach (var download in _blockDownloads)
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
        _blockDownloads.Clear();
        if (_blockCursor is not null)
        {
            var remaining = _blockCursor.GetRequest(_blockCursor.Remaining);
            _download.Cancel(remaining);
        }
    }

    public ValueTask DisposeAsync()
    {
        CancelAll();
        return _messagePipe.CompleteAsync();
    }
}
