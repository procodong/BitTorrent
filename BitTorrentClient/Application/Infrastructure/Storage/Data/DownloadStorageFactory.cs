using BencodeNET.Torrents;
using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
internal static class DownloadStorageFactory
{
    public static StorageStream CreateMultiFileStorage(string path, IReadOnlyList<FileData> files)
    {
        var createdFiles = new List<StreamData>(files.Count);
        long createdBytes = 0;
        foreach (var file in files)
        {
            var filePath = Path.Combine(path, file.Path);
            var handle = new Lazy<Task<IRandomAccesStream>>(() => Task.Run(() => CreateStream(filePath, file.Size)), true);
            createdFiles.Add(new(createdBytes, file.Size, handle));
            createdBytes += file.Size;
        }
        return new(createdFiles, createdBytes);
    }
    
    public static StorageStream CreateSingleFileStorage(FileData file)
    {
        var handle = new Lazy<Task<IRandomAccesStream>>(() => Task.Run(() => CreateStream(file.Path, file.Size)), true);
        var streamData = new StreamData(0, file.Size, handle);
        return new([streamData], file.Size);
    }

    private static IRandomAccesStream CreateStream(string path, long size)
    {
        var createdFile = File.Open(path, new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.OpenOrCreate,
            Options = FileOptions.Asynchronous | FileOptions.RandomAccess,
        });
        if (createdFile.Length != size)
        {
            createdFile.SetLength(size);
        }
        return new FileRandomAccessStream(createdFile);
    }
}
