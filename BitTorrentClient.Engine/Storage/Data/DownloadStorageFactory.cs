using BitTorrentClient.Helpers.Streams;
using BitTorrentClient.Core.Presentation.Torrent;

namespace BitTorrentClient.Engine.Storage.Data;
public static class DownloadStorageFactory
{
    public static StorageStream CreateMultiFileStorage(string path, FileData[] files)
    {
        var createdFiles = new List<StreamData>(files.Length);
        long createdBytes = 0;
        foreach (var file in files)
        {
            var filePath = Path.Combine(path, file.Path);
            var stream = new Lazy<Task<IRandomAccesStream>>(() => Task.Run(() => CreateStream(filePath, file.Size)), true);
            createdFiles.Add(new(createdBytes, file.Size, stream));
            createdBytes += file.Size;
        }
        return new(createdFiles, createdBytes);
    }
    
    public static StorageStream CreateSingleFileStorage(FileData file)
    {
        var stream = new Lazy<Task<IRandomAccesStream>>(() => Task.Run(() => CreateStream(file.Path, file.Size)), true);
        var streamData = new StreamData(0, file.Size, stream);
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
