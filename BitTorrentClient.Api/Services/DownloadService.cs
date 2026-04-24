using System.IO.Pipelines;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Api.Information;
using BitTorrentClient.Api.Interface;
using BitTorrentClient.Engine.Infrastructure.Downloads.Interface;
using BitTorrentClient.Engine.Storage.Data;
using BitTorrentClient.Core.Presentation.Torrent;
using BitTorrentClient.Engine.Models.Config;
using EngineSettings = BitTorrentClient.Engine.Models.Config.DownloadSettings;
using ClientSettings = BitTorrentClient.Api.Interface.DownloadSettings;

namespace BitTorrentClient.Api.Services;

internal class DownloadService : IDownloadService
{
    private readonly IDownloadRepository _downloads;
    private readonly CancellationTokenSource _canceller;
    private readonly Task _clientTask;

    public DownloadService(IDownloadRepository downloads, CancellationTokenSource canceller, Task clientTask)
    {
        _downloads = downloads;
        _canceller = canceller;
        _clientTask = clientTask;
    }
    
    public async Task<IDownloadHandle> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, ClientSettings clientSettings, CancellationToken cancellationToken = default)
    {
        await using var file = File.Open(downloadFile.FullName, new FileStreamOptions
        {
            Options = FileOptions.Asynchronous
        });
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream, cancellationToken);
        var path = Path.Combine(targetDirectory.FullName, torrent.DisplayName);
        var files = torrent.FileMode == TorrentFileMode.Multi
            ? torrent.Files.Select(v => new FileData(v.FileName, v.FileSize)).ToArray()
            : [new(torrent.File.FileName, torrent.File.FileSize)];
        var data = new DownloadData(torrent.GetInfoHashBytes(), torrent.Pieces, torrent.Trackers.Select(v => v.Select(s => new Uri(s)).ToArray()).ToArray(), files, (int)torrent.PieceSize, torrent.NumberOfPieces, torrent.TotalSize, clientSettings.Name ?? torrent.DisplayName, path);
        var storage = CreateStorage(data);
        var settings = new EngineSettings
        {
            TargetDataTransferPerSecond = new(clientSettings.DownloadLimit, clientSettings.UploadLimit),
            MaxParallelPeers = clientSettings.MaxParallelPeers,
            Strategy = (PieceSelectionStrategyType)clientSettings.Strategy
        };
        var handle = _downloads.AddDownload(data, storage, settings);
        return new DownloadHandle(handle.Writer, handle.State);
    }

    public IDownloadHandle AddDownload(DownloadModel model)
    {
        var storage = CreateStorage(model.Data);
        var handle = _downloads.AddDownload(model.Data, storage, model.Settings);
        return new DownloadHandle(handle.Writer, handle.State);
    }

    public bool RemoveDownload(ReadOnlyMemory<byte> id)
    {
        return _downloads.RemoveDownload(new(id));
    }

    public IEnumerable<DownloadUpdate> GetDownloadUpdates()
    {
        foreach (var download in _downloads.GetDownloads())
        {
            var state = download.State;
            yield return new(state.Download.Data.Name, state.DataTransfer.Fetch(), state.TransferRate, state.Download.Data.Size, (DownloadExecutionState)state.ExecutionState, state.Download.Data.InfoHash);
        }
    }

    public IEnumerable<DownloadModel> GetDownloads()
    {
        return _downloads.GetDownloads().Select(download => new DownloadModel(download.State.Download.Data, download.State.Download.Settings));
    }

    private static StorageStream CreateStorage(DownloadData data)
    {
        return data.Files.Length != 1
            ? DownloadStorageFactory.CreateMultiFileStorage(data.SavePath, data.Files)
            : DownloadStorageFactory.CreateSingleFileStorage(new(Path.Combine(data.SavePath, data.Files[0].Path), data.Files[0].Size));
    }

    public void Dispose()
    {
        _canceller.Cancel();
        _clientTask.GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await _canceller.CancelAsync();
        await _clientTask;
    }
}