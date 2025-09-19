using BitTorrentClient.Api.Information;

namespace BitTorrentClient.Api.Downloads;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    Task<IDownloadHandle> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null);
    IDownloadHandle AddDownload(DownloadModel data);
    bool RemoveDownload(ReadOnlyMemory<byte> id);
    IEnumerable<DownloadModel> GetDownloads();
    IEnumerable<DownloadUpdate> GetDownloadUpdates();
}
