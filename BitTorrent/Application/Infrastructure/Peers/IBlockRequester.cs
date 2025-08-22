using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Messages;

namespace BitTorrentClient.Application.Infrastructure.Peers;
public interface IBlockRequester
{
    IEnumerable<PieceRequest> DrainRequests();
    bool TryGetBlock(PieceRequest request, out Stream stream);
    Task SaveBlockAsync(BlockData data, CancellationToken cancellationToken = default);
    bool TryRequestDownload(out Block block);
}
