using BencodeNET.Torrents;

namespace BitTorrentClient.Storage;
public class DownloadStorageFactory
{
    private readonly int _handlesPerMb;
    private readonly int _maxHandleCount;

    public DownloadStorageFactory(int handlesPerMb, int maxHandleCount)
    {
        _handlesPerMb = handlesPerMb;
        _maxHandleCount = maxHandleCount;
    }

    public DownloadStorage CreateMultiFileStorage(string path, MultiFileInfoList files, int pieceSize)
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
            var handleFactory = new FileHandleFactory(filePath, file.FileSize);
            var handles = new StreamHandlePool(HandleCount(file.FileSize), handleFactory);
            createdFiles.Add(new(createdBytes, file.FileSize, handles));
            createdBytes += file.FileSize;
        }
        return new(pieceSize, createdFiles);
    }

    public DownloadStorage CreateSingleFileStorage(string path, SingleFileInfo file, int pieceSize)
    {
        var handleFactory = new FileHandleFactory(Path.Combine(path, file.FileName), file.FileSize);
        var pool = new StreamHandlePool(HandleCount(file.FileSize), handleFactory);
        var streamData = new StreamData(0, file.FileSize, pool);
        return new(pieceSize, [streamData]);
    }

    private int HandleCount(long fileSize)
    {
        int handleCount = (int)(fileSize / 1_000_000 * _handlesPerMb);
        if (handleCount == 0) handleCount = 1;
        return int.Min(handleCount, _maxHandleCount);
    }
}
