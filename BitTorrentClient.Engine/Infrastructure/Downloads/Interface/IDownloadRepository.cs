using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Models.Config;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Infrastructure.Downloads.Interface;

public interface IDownloadRepository : IDisposable, IAsyncDisposable
{
    DownloadHandle AddDownload(DownloadData data, StorageStream storage, DownloadSettings settings);
    bool RemoveDownload(ByteId id);
    IEnumerable<DownloadHandle> GetDownloads();
}