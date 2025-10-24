using BitTorrentClient.Engine.Infrastructure.Downloads;
using BitTorrentClient.Engine.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Engine.Storage.Interface;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Storage.Distribution;

public sealed class BlockDistributor : IBlockRequester
{
    private readonly List<Block> _requests;
    private readonly SynchronizedBlockAssigner _downloader;
    private readonly DownloadState _state;
    private readonly BlockStorage _storage;
    private BlockCursor _blockCursor;

    public BlockDistributor(SynchronizedBlockAssigner downloader, DownloadState state, BlockStorage storage)
    {
        _state = state;
        _requests = new(state.Download.Config.RequestQueueSize);
        _downloader = downloader;
        _storage = storage;
        _blockCursor = new(default);
    }
    
    public void ClearRequests()
    {
        Block? currentBlock = default;
        foreach (var block in _requests)
        {
            currentBlock ??= block;
            var blockEnd = currentBlock.Value.Begin + currentBlock.Value.Length;
            if (blockEnd == block.Begin)
            {
                currentBlock = currentBlock.Value with
                {
                    Length = currentBlock.Value.Length + block.Length
                };
            }
            else
            {
                _downloader.Cancel(currentBlock.Value);
                currentBlock = block;
            }
        }
        _requests.Clear();
    }

    public bool TryGetBlock(BlockRequest request, out Stream stream)
    {
        ValidateRequest(request);
        if (!_state.DownloadedPieces[request.Index])
        {
            stream = null!;
            return false;
        }
        stream = _storage.RequestBlock(request);
        return true;
    }

    private void ValidateRequest(BlockRequest request)
    {
        var size = PieceSize(request.Index);
        var end = request.Begin + request.Length;
        if (end > size || end < 0)
        {
            throw new BadPeerException(PeerErrorReason.InvalidRequest);
        }
    }

    private int PieceSize(int piece)
    {
        return (int)long.Min(_state.Download.Data.PieceSize, _state.Download.Data.Size - piece * _state.Download.Data.PieceSize);
    }

    public async Task SaveBlockAsync(BlockData data, CancellationToken cancellationToken = default)
    {
        var blockIndex = _requests.FindIndex(b => b == data.Request);
        if (blockIndex == -1)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        var block = _requests[blockIndex];
        try
        {
            await _storage.SaveBlockAsync(data.Stream, block, cancellationToken);
            _state.DataTransfer.AtomicAddDownload(data.Request.Length);
        }
        catch (InvalidDataException)
        {
            _downloader.Cancel(block);

            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        finally
        {
            _requests.RemoveAt(blockIndex);
        }
    }

    public bool TryRequestDownload(LazyBitArray pieces, out Block block)
    {
        if (_requests.Count == _state.Download.Config.RequestQueueSize)
        {
            block = default;
            return false;
        }
        var request = _blockCursor.GetRequest(_state.Download.Config.RequestSize);
        if (request.Length == 0)
        {
            if (_downloader.TryAssignBlock(pieces, out var newBlock))
            {
                _blockCursor = new(newBlock);
                request = _blockCursor.GetRequest(_state.Download.Config.RequestSize);
            }
            else
            {
                block = default;
                return false;
            }
        }
        _requests.Add(request);
        block = request;
        return true;
    }
}