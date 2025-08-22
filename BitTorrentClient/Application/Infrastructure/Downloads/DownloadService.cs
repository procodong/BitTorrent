using System.IO.Pipelines;
using BencodeNET.IO;
using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Interfaces;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Application;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

public class DownloadService : IDownloadService
{
    private readonly IDownloadCollection _downloads;
    private readonly ILogger _logger;

    public DownloadService(IDownloadCollection downloads, ILogger logger)
    {
        _downloads = downloads;
        _logger = logger;
    }
    
    public async Task<ReadOnlyMemory<byte>> AddDownloadAsync(FileInfo downloadFile, DirectoryInfo targetDirectory, string? name = null)
    {
        await using var file = File.Open(downloadFile.FullName, new FileStreamOptions
        {
            Options = FileOptions.Asynchronous,
        });
        var parser = new TorrentParser();
        var stream = new PipeBencodeReader(PipeReader.Create(file));
        var torrent = await parser.ParseAsync(stream);
        
        StorageStream storage = torrent.Files is not null
            ? DownloadStorageFactory.CreateMultiFileStorage(targetDirectory.FullName, torrent.Files)
            : DownloadStorageFactory.CreateSingleFileStorage(targetDirectory.FullName, torrent.File);
        _ = _downloads.AddDownloadAsync(torrent, storage, name).Catch(ex => _logger.LogError(ex, "Adding download {}", ex));
        return torrent.OriginalInfoHashBytes;
    }

    public Task<bool> RemoveDownloadAsync(ReadOnlyMemory<byte> id)
    {
        return _downloads.RemoveDownloadAsync(id);
    }

    public IEnumerable<DownloadUpdate> GetUpdates() => _downloads.GetDownloadState();
    
    public void Dispose()
    {
        _downloads.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _downloads.DisposeAsync();
    }
}