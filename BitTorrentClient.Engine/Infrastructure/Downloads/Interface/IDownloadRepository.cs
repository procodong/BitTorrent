using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Core.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Downloads.Interface;

public interface IDownloadRepository : IDisposable, IAsyncDisposable
{
    DownloadHandle AddDownload(DownloadData data, StorageStream storage);
    bool RemoveDownload(DownloadId id);
    IEnumerable<DownloadHandle> GetDownloads();
}