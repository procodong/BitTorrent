using BitTorrentClient.Data;

namespace BitTorrentClient.Api;

public interface IDownloadController
{
    public Task ResumeAsync(CancellationToken cancellationToken = default);
    public Task PauseAsync(CancellationToken cancellationToken = default);
    public DownloadUpdate State { get; }
    public DownloadModel Download { get; }
}