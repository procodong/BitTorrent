using System.Threading.Channels;
using BitTorrentClient.Api;
using BitTorrentClient.Data;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using DownloadExecutionState = BitTorrentClient.Engine.Models.Downloads.DownloadExecutionState;

namespace BitTorrentClient.Services;

internal class DownloadController : IDownloadController
{
    private readonly ChannelWriter<DownloadExecutionState> _stateWriter;
    private readonly DownloadState _downloadState;

    public DownloadController(ChannelWriter<DownloadExecutionState> stateWriter, DownloadState downloadState)
    {
        _stateWriter = stateWriter;
        _downloadState = downloadState;
    }
    
    public DownloadUpdate State => new(_downloadState.GetUpdate());
    public DownloadModel Download => new(_downloadState.Download.Data);

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        await _stateWriter.WriteAsync(DownloadExecutionState.PausedByUser, cancellationToken);
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        await _stateWriter.WriteAsync(DownloadExecutionState.Running, cancellationToken);
    }

}