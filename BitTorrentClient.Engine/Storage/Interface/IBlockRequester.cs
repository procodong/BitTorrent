using BitTorrentClient.Engine.Storage.Distribution;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Core.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Storage.Interface;
public interface IBlockRequester
{
    void ClearRequests();
    bool TryGetBlock(BlockRequest request, out Stream stream);
    Task SaveBlockAsync(BlockData data, CancellationToken cancellationToken = default);
    bool TryRequestDownload(LazyBitArray pieces, out Block block);
}
