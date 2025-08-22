using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Peers;
using BitTorrentClient.Application.Infrastructure.Peers.Exceptions;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Application;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Application.Infrastructure.Storage.Distribution;

public class BlockDistributor : IBlockRequester
{
    private readonly List<Block> _requests;
    private readonly Downloader _downloader;
    private readonly BlockStorage _storage;
    private readonly ChannelWriter<DownloadExecutionState> _downloadStateWriter;
    private BlockCursor _blockCursor;

    public BlockDistributor(Downloader downloader, BlockStorage storage, ChannelWriter<DownloadExecutionState> downloadStateWriter)
    {
        _requests = new(downloader.Config.RequestQueueSize);
        _downloader = downloader;
        _storage = storage;
        _blockCursor = new(default);
        _downloadStateWriter = downloadStateWriter;
    }
    
    public IEnumerable<BlockRequest> DrainRequests()
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
        }
        catch (InvalidDataException)
        {
            lock (_downloader)
            {
                _downloader.Cancel(block);
            }

            throw new BadPeerException(PeerErrorReason.InvalidPiece);
        }
        catch (IOException)
        {
            await _downloadStateWriter.WriteAsync(DownloadExecutionState.PausedAutomatically, cancellationToken);
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