using BitTorrentClient.Helpers.DataStructures;
using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Engine.Infrastructure.Peers.Interface;
public interface IPeer
{
    bool Downloading { get; set; }
    bool WantsToUpload { get; set; }
    bool Uploading { get; set; }
    bool WantsToDownload { get; set; }
    DataTransferVector TransferLimit { get; set; }
    LazyBitArray DownloadedPieces { get; set; }
    Task RequestUploadAsync(BlockRequest request, CancellationToken cancellationToken = default);
    Task CancelUploadAsync(BlockRequest request);
    Task RequestDownloadAsync(BlockData blockData, CancellationToken cancellationToken = default);
    Task UpdateAsync(CancellationToken cancellationToken = default);
    void NotifyHavePiece(int piece);
}
