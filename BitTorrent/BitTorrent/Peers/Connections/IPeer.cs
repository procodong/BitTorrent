using BitTorrentClient.Models.Messages;
using BitTorrentClient.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.BitTorrent.Peers.Connections;
public interface IPeer : IAsyncDisposable
{
    bool Downloading { get; set; }
    bool WantsToDownload { get; set; }
    bool Uploading { get; set; }
    bool WantsToUpload { get; set; }
    LazyBitArray BitArray { get; set; }
    Task UploadAsync(PieceRequest request, CancellationToken cancellationToken = default);
    Task CancelUploadAsync(PieceRequest request);
    Task DownloadAsync(BlockData blockData, CancellationToken cancellationToken = default);
    Task UpdateAsync(CancellationToken cancellationToken = default);
    void NotifyHavePiece(int piece);
}
