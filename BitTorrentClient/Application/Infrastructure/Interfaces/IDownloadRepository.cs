namespace BitTorrentClient.Application.Infrastructure.Interfaces;

public interface IDownloadRepository
{
    Task AddTorrentAsync(FileInfo torrentPath, DirectoryInfo targetPath, string? name = null);
    Task RemoveTorrentAsync(string name);
    Task RemoveTorrentAsync(int index);
}
