using BitTorrentClient.Api.Information;

namespace BitTorrentClient.Api.Downloads;

public interface IDownloadHandle
{
    public Task ResumeAsync(CancellationToken cancellationToken = default);
    public Task PauseAsync(CancellationToken cancellationToken = default);
    public DownloadUpdate State { get; }
    public DownloadModel Download { get; }
}