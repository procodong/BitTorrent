using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Application.Infrastructure.Interfaces;
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
