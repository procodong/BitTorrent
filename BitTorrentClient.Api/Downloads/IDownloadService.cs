using BitTorrentClient.Api.Information;

namespace BitTorrentClient.Api.Downloads;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    Task<IDownloadController> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null);
    Task AddDownloadAsync(DownloadModel data);
    Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id);
    IEnumerable<IDownloadController> GetDownloads();
}
