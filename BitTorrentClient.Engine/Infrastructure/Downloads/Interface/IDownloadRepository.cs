using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Protocol.Presentation.Torrent;
using BitTorrentClient.Protocol.Transport.PeerWire.Handshakes;

namespace BitTorrentClient.Engine.Infrastructure.Downloads.Interface;

public interface IDownloadRepository : IDisposable, IAsyncDisposable
{
    DownloadHandle AddDownload(DownloadData data, StorageStream storage);
    bool RemoveDownload(DownloadId id);
    IEnumerable<DownloadHandle> GetDownloads();
}