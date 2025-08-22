using BitTorrentClient.Application.Infrastructure.Storage.Distribution;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Downloads.Interface;

public interface IDownloadController
{
    public Task ResumeAsync(CancellationToken cancellationToken = default);
    public Task PauseAsync(CancellationToken cancellationToken = default);
    public DownloadUpdate State { get; }
    public DownloadData Download { get; }
}