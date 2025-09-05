using System.Threading.Channels;
using BitTorrentClient.Api.Downloads;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Engine.Infrastructure.Downloads;
using DownloadExecutionState = BitTorrentClient.Engine.Models.Downloads.DownloadExecutionState;

namespace BitTorrentClient.Api.Services;

internal class DownloadHandle : IDownloadHandle
{
    private readonly ChannelWriter<DownloadExecutionState> _stateWriter;
    private readonly DownloadState _downloadState;

    public DownloadHandle(ChannelWriter<DownloadExecutionState> stateWriter, DownloadState downloadState)
    {
        _stateWriter = stateWriter;
        _downloadState = downloadState;
    }
    
    public DownloadUpdate State => new(_downloadState.Download.Data.Name, _downloadState.DataTransfer.Fetch(), _downloadState.TransferRate, Download.Data.Size, (Information.DownloadExecutionState)_downloadState.ExecutionState, Download.Data.InfoHash);
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