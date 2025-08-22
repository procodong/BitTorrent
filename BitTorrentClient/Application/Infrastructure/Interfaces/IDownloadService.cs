using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Interfaces;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    Task<ReadOnlyMemory<byte>> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null);
    Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id);
    IEnumerable<DownloadUpdate> GetUpdates();
}
