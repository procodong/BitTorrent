using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Downloads.Interface;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    Task<IDownloadController> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null);
    void AddDownload(DownloadData data);
    Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id);
    IEnumerable<IDownloadController> GetDownloads();
    IDownloadController GetDownload(ReadOnlyMemory<byte> id);
}
