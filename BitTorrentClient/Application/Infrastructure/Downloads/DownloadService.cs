using System.IO.Pipelines;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Downloads.Interface;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

internal class DownloadService : IDownloadService
{
    private readonly IDownloadCollection _downloads;
    private readonly ILogger _logger;

    public DownloadService(IDownloadCollection downloads, ILogger logger)
    {
        _downloads = downloads;
        _logger = logger;
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
        return await _downloads.AddDownloadAsync(data, storage);
    }

    public void AddDownload(DownloadData data)
    {
        var storage = CreateStorage(data);
        _ = _downloads.AddDownloadAsync(data, storage);
    }

    private static StorageStream CreateStorage(DownloadData data)
    {
        return data.Files.Length != 1
            ? DownloadStorageFactory.CreateMultiFileStorage(data.SavePath, data.Files)
            : DownloadStorageFactory.CreateSingleFileStorage(new(Path.Combine(data.SavePath, data.Files[0].Path), data.Files[0].Size));
    }

    public Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id)
    {
        return _downloads.RemoveDownloadAsync(id);
    }

    public IEnumerable<IDownloadController> GetDownloads() => _downloads.GetDownloads();

    public IDownloadController GetDownload(ReadOnlyMemory<byte> id)
    {
        return _downloads.GetDownloadController(id);
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