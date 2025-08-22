using BencodeNET.Torrents;
using BitTorrentClient.Helpers.Streams;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public static class DownloadStorageFactory
{
    public static StorageStream CreateMultiFileStorage(string path, MultiFileInfoList files)
    {
        var createdFiles = new List<StreamData>(files.Count);
        var createdDirectories = new HashSet<string>();
        long createdBytes = 0;
        foreach (var file in files)
        {
            if (file.Path.Count != 1)
            {
                var directoryPath = string.Join(Path.PathSeparator, file.Path.Take(file.Path.Count - 1));
                if (!createdDirectories.Contains(directoryPath))
                {
                    Directory.CreateDirectory(Path.Combine(path, directoryPath));
                    createdDirectories.Add(directoryPath);
                }
            }
            var filePath = Path.Combine(path, file.FullPath);
            var handle = new Lazy<Task<IRandomAccesStream>>(() => Task.Run(() => CreateStream(filePath, file.FileSize)), true);
            createdFiles.Add(new(createdBytes, file.FileSize, handle));
            createdBytes += file.FileSize;
        }
        return new(createdFiles, createdBytes);
    }
    
    public static StorageStream CreateSingleFileStorage(string path, SingleFileInfo file)
    {
        var filePath = Path.Combine(path, file.FileName);
        var handle = new Lazy<Task<IRandomAccesStream>>(() => Task.Run(() => CreateStream(filePath, file.FileSize)), true);
        var streamData = new StreamData(0, file.FileSize, handle);
        return new([streamData], file.FileSize);
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
