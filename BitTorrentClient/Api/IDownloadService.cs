using BitTorrentClient.Data;

namespace BitTorrentClient.Api;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    Task<IDownloadController> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null);
    void AddDownload(DownloadModel data);
    Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id);
    IEnumerable<IDownloadController> GetDownloads();
    IDownloadController GetDownload(ReadOnlyMemory<byte> id);
}
