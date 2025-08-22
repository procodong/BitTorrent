using BitTorrentClient.Models.Messages;
using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Application.Events.Handling.Peers;
public interface IPeer
{
    bool Downloading { get; set; }
    bool WantsToDownload { get; set; }
    bool Uploading { get; set; }
    bool WantsToUpload { get; set; }
    LazyBitArray DownloadedPieces { get; set; }
    Task RequestUploadAsync(BlockRequest request, CancellationToken cancellationToken = default);
    Task CancelUploadAsync(BlockRequest request);
    Task RequestDownloadAsync(BlockData blockData, CancellationToken cancellationToken = default);
    Task UpdateAsync(CancellationToken cancellationToken = default);
    void NotifyHavePiece(int piece);
}
