using BitTorrentClient.Engine.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Infrastructure.Storage.Interface;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Distribution;

public class BlockDistributor : IBlockRequester
{
    private readonly List<Block> _requests;
    private readonly Downloader _downloader;
    private readonly BlockStorage _storage;
    private BlockCursor _blockCursor;

    public BlockDistributor(Downloader downloader, BlockStorage storage)
    {
        _requests = new(downloader.Config.RequestQueueSize);
        _downloader = downloader;
        _storage = storage;
        _blockCursor = new(default);
    }
    
    public IEnumerable<BlockRequest> DrainRequests()
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
                lock (_downloader)
                {
                    _downloader.Cancel(currentBlock.Value);
                }
                currentBlock = block;
            }
            yield return block;
        }
        _requests.Clear();
    }

    public bool TryGetBlock(BlockRequest request, out Stream stream)
    {
        if (!_downloader.DownloadedPieces[request.Index])
        {
            stream = null!;
            return false;
        }
        stream = _storage.RequestBlock(request);
        return true;
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
            _downloader.RegisterDownloaded(data.Request.Length);
        }
        catch (InvalidDataException)
        {
            lock (_downloader)
            {
                _downloader.Cancel(block);
            }

            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        finally
        {
            _requests.RemoveAt(blockIndex);
        }
    }

    public bool TryRequestDownload(LazyBitArray pieces, out Block block)
    {
        if (_requests.Count == _downloader.Config.RequestQueueSize)
        {
            block = default;
            return false;
        }
        var request = _blockCursor.GetRequest(_downloader.Config.RequestSize);
        if (request.Length == 0)
        {
            lock (_downloader)
            {
                if (_downloader.TryAssignBlock(pieces, out var newBlock))
                {
                    _blockCursor = new(newBlock);
                    request = _blockCursor.GetRequest(_downloader.Config.RequestSize);
                }
                else
                {
                    block = default;
                    return false;
                }
            }
        }
        _requests.Add(request);
        block = request;
        return true;
    }
}