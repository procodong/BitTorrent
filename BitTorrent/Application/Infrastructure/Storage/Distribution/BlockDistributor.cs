using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;

public class BlockDistributor : IBlockRequester
{
    private readonly List<Block> _requests;
    private readonly Downloader _downloader;
    private BlockCursor _blockCursor;

    public BlockDistributor(Downloader downloader)
    {
        _requests = new(downloader.Config.RequestQueueSize);
        _downloader = downloader;
        _blockCursor = new(default);
    }
    
    public IEnumerable<PieceRequest> DrainRequests()
    {
        Block? currentBlock = default;
        foreach (var block in _requests)
        {
            currentBlock ??= block;
            int blockEnd = currentBlock.Value.Begin + currentBlock.Value.Length;
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

    public bool TryGetBlock(PieceRequest request, out Stream stream)
    {
        if (!_downloader.DownloadedPieces[request.Index])
        {
            stream = null!;
            return false;
        }
        lock (_downloader)
        {
            stream = _downloader.RequestBlock(request);
        }
        return true;
    }

    public async Task SaveBlockAsync(BlockData data, CancellationToken cancellationToken = default)
    {
        var blockIndex = _requests.FindIndex(b => b == data.Request);
        if (blockIndex == -1)
        {
            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        await _downloader.SaveBlockAsync(data.Stream, _requests[blockIndex], cancellationToken);
        _requests.RemoveAt(blockIndex);
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
                if (_downloader.TryAssignBlock(pieces, out Block newBlock))
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