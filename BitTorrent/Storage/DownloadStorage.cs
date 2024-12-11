using BencodeNET.Torrents;
using BitTorrent.Storage;
using BitTorrent.Storage.DownloadFiles;
using BitTorrent.Utils;

namespace BitTorrent.Storage;
public class DownloadStorage(int pieceSize, List<StreamData> saves) : IDisposable, IAsyncDisposable
{
    private readonly int _pieceSize = pieceSize;
    private readonly List<StreamData> _saves = saves;

    public static DownloadStorage CreateFiles(string path, MultiFileInfoList files, int pieceSize)
    {
        var createdFiles = new List<StreamData>(files.Count);
        var createdDirectories = new HashSet<string>();
        long createdBytes = 0;
        foreach (var file in files)
        {
            if (file is null) continue;
            var directoryPath = string.Join(Path.PathSeparator, file.Path.Take(file.Path.Count - 1));
            if (!createdDirectories.Contains(directoryPath) && file.Path.Count != 1)
            {
                Directory.CreateDirectory(Path.Combine(path, directoryPath));
                createdDirectories.Add(directoryPath);
            }
            var filePath = Path.Combine(path, file.FullPath);;
            var createdFile = File.Create(filePath, 1 << 12, FileOptions.Asynchronous);
            createdFile.SetLength(file.FileSize);
            createdFiles.Add(new(createdFile, createdBytes, new(1, 1)));
            createdBytes += file.FileSize;
        }
        return new(pieceSize, createdFiles);
    }

    public PieceStream GetStream(int pieceIndex, int offset, int length) => new(GetParts(length, pieceIndex, offset), length);

    private IEnumerable<StreamPart> GetParts(int length, int pieceIndex, int offset)
    {
        long byteOffset = pieceIndex * _pieceSize + offset;
        int bytesRead = 0;
        int start = Search(byteOffset);
        for (int i = start; bytesRead <= length; i++)
        {
            StreamData file = _saves[i];
            long position = i == start ? byteOffset - file.ByteOffset : 0;
            int readLen = (int)long.Min(length - bytesRead, file.Stream.Length - position);
            yield return new(file, readLen, position);
            bytesRead += readLen;
        }
    }

    private int Search(long byteOffset)
    {
        int low = 0;
        int high = _saves.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (mid < _saves.Count - 1 && byteOffset >= _saves[mid].ByteOffset && byteOffset < _saves[mid + 1].ByteOffset)
            {
                return mid;
            }

            if (byteOffset < _saves[mid].ByteOffset)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        throw new Exception("Invalid piece index");
    }

    public void Dispose()
    {
        foreach (var fileData in _saves)
        {
            fileData.Stream.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var fileData in _saves)
        {
            await fileData.Stream.DisposeAsync();
        }
    }
}
