using BitTorrentClient.Api.Information;

namespace BitTorrentClient.Api.Interface;

public interface IDownloadService : IDisposable, IAsyncDisposable
{
    Task<IDownloadHandle> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, DownloadSettings settings, CancellationToken cancellationToken = default);
    IDownloadHandle AddDownload(DownloadModel model);
    bool RemoveDownload(ReadOnlyMemory<byte> id);
    IEnumerable<DownloadModel> GetDownloads();
    IEnumerable<DownloadUpdate> GetDownloadUpdates();
}

public enum PieceSelectionStrategy
{
    RarestFirst,
    Sequential
}

public class DownloadSettings
{
    public string? Name { get; init; }
    public long DownloadLimit { get; set; } = long.MaxValue;
    public long UploadLimit { get; set; } = long.MaxValue;
    public PieceSelectionStrategy Strategy { get; set; } = PieceSelectionStrategy.RarestFirst;
    public int MaxParallelPeers { get; set; } = 30;
}