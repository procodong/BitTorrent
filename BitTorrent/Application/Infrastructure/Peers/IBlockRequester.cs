using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Application.Infrastructure.Peers;
public interface IBlockRequester
{
    IEnumerable<BlockRequest> DrainRequests();
    bool TryGetBlock(BlockRequest request, out Stream stream);
    Task SaveBlockAsync(BlockData data, CancellationToken cancellationToken = default);
    bool TryRequestDownload(LazyBitArray pieces, out Block block);
}
