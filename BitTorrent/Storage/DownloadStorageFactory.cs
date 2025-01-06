using BencodeNET.Torrents;

namespace BitTorrentClient.Storage;
public static class DownloadStorageFactory
{
    public static DownloadStorage CreateMultiFileStorage(string path, MultiFileInfoList files, int pieceSize)
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
            var handle = new Lazy<Task<StreamHandle>>(() => Task.Run(() => CreateStream(filePath, file.FileSize)), true);
            createdFiles.Add(new(createdBytes, file.FileSize, handle));
            createdBytes += file.FileSize;
        }
        return new(pieceSize, createdFiles);
    }

    public static DownloadStorage CreateSingleFileStorage(string path, SingleFileInfo file, int pieceSize)
    {
        var filePath = Path.Combine(path, file.FileName);
        var handle = new Lazy<Task<StreamHandle>>(() => Task.Run(() => CreateStream(filePath, file.FileSize)), true);
        var streamData = new StreamData(0, file.FileSize, handle);
        return new(pieceSize, [streamData]);
    }

    private static StreamHandle CreateStream(string path, long size)
    {
        var createdFile = File.Open(path, new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Share = FileShare.ReadWrite,
            Mode = FileMode.OpenOrCreate,
            Options = FileOptions.Asynchronous,
        });
        if (createdFile.Length != size)
        {
            createdFile.SetLength(size);
        }
        return new(new(1, 1), createdFile);
    }
}
