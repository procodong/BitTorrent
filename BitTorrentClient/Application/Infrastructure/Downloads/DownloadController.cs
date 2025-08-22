using System.Threading.Channels;
using BitTorrentClient.Application.Infrastructure.Downloads.Interface;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

internal class DownloadController : IDownloadController
{
    private readonly ChannelWriter<DownloadExecutionState> _stateWriter;
    private readonly DownloadState _downloadState;

    public DownloadController(ChannelWriter<DownloadExecutionState> stateWriter, DownloadState downloadState)
    {
        _stateWriter = stateWriter;
        _downloadState = downloadState;
    }
    
    public DownloadUpdate State => _downloadState.GetUpdate();
    public DownloadData Download => _downloadState.Download.Data;

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        await _stateWriter.WriteAsync(DownloadExecutionState.PausedByUser, cancellationToken);
    }

    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        await _stateWriter.WriteAsync(DownloadExecutionState.Running, cancellationToken);
    }

}