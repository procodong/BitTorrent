using System.Collections.Concurrent;
using System.IO.Pipelines;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Api;
using BitTorrentClient.Data;
using BitTorrentClient.Engine.Infrastructure.Downloads.Interface;
using BitTorrentClient.Engine.Infrastructure.Storage.Data;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Protocol.Presentation.Torrent;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Services;

internal class DownloadService : IDownloadService
{
    private readonly IDownloadRepository _downloads;
    private readonly ConcurrentDictionary<DownloadId, IDownloadController> _controllers;
    private readonly ILogger _logger;

    public DownloadService(IDownloadRepository downloads, ILogger logger)
    {
        _downloads = downloads;
        _logger = logger;
        _controllers = new();
    }
    
    public async Task<IDownloadController> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null)
    {
        await using var file = File.Open(downloadFile.FullName, new FileStreamOptions
        {
            Options = FileOptions.Asynchronous,
            
        });
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream);
        var path = Path.Combine(targetDirectory.FullName, torrent.DisplayName);
        var files = torrent.FileMode == TorrentFileMode.Multi
            ? torrent.Files.Select(v => new FileData(v.FileName, v.FileSize)).ToArray()
            : [new(torrent.File.FileName, torrent.File.FileSize)];
        var data = new DownloadData(torrent.GetInfoHashBytes(), torrent.Pieces, torrent.Trackers.Select(v => v.ToArray()).ToArray(), files, (int)torrent.PieceSize, torrent.NumberOfPieces, torrent.TotalSize, name ?? torrent.DisplayName, path);
        var storage = CreateStorage(data);
        var handle = await _downloads.AddDownloadAsync(data, storage);
        return new DownloadController(handle.Writer, handle.State);
    }

    public void AddDownload(DownloadModel data)
    {
        var storage = CreateStorage(data.Data);
        _ = _downloads.AddDownloadAsync(data.Data, storage).Catch(ex => _logger.LogError(ex, "Failed to add download {}", ex));
    }

    private static StorageStream CreateStorage(DownloadData data)
    {
        return data.Files.Length != 1
            ? DownloadStorageFactory.CreateMultiFileStorage(data.SavePath, data.Files)
            : DownloadStorageFactory.CreateSingleFileStorage(new(Path.Combine(data.SavePath, data.Files[0].Path), data.Files[0].Size));
    }

    public async Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id)
    {
        var successful = await _downloads.RemoveDownloadAsync(new(id));
        if (successful) _controllers.Remove(new(id), out _);
        return successful;
    }

    public IEnumerable<IDownloadController> GetDownloads()
    {
        foreach (var download in _downloads.GetDownloads())
        {
            yield return _controllers.GetOrAdd(new(download.State.Download.Data.InfoHash), _ => new DownloadController(download.Writer, download.State));
        }
    }

    public IDownloadController GetDownload(ReadOnlyMemory<byte> id)
    {
        var handle = _downloads.GetDownload(new(id));
        return new DownloadController(handle.Writer, handle.State);
    }
    
    public void Dispose()
    {
        _downloads.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _downloads.DisposeAsync();
    }
}