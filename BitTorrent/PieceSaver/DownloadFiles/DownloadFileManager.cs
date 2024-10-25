using BencodeNET.Torrents;
using BitTorrent.PieceSaver.DownloadFiles;
using BitTorrent.Utils;

namespace BitTorrent.Files.DownloadFiles;
public class DownloadFileManager(int pieceSize) : IDisposable, IAsyncDisposable
{
    private readonly int PieceSize = pieceSize;
    private readonly List<FileData> Files = [];

    public void Create(string path, MultiFileInfoList files)
    {
        var createdDirectories = new HashSet<string>();
        long createdBytes = 0;
        foreach (var file in files)
        {
            var directoryPath = string.Join(Path.PathSeparator, file.Path);
            if (!createdDirectories.Contains(directoryPath))
            {
                Directory.CreateDirectory(Path.Combine(path, directoryPath));
                createdDirectories.Add(directoryPath);
            }
            var filePath = Path.Combine(path, file.FullPath);
            var createdFile = File.Create(filePath);
            createdFile.SetLength(file.FileSize);
            Files.Add(new(createdFile, createdBytes, new(1, 1)));
            createdBytes += file.FileSize;
        }
    }

    public void Dispose()
    {
        foreach (FileData fileData in Files)
        {
            fileData.File.Close();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (FileData fileData in Files)
        {
            await fileData.File.DisposeAsync();
        }
    }

    public PartStream Read(int index) => GetStream(index, 0, PieceSize);

    public PartStream GetStream(int pieceIndex, int offset, int length) => new(GetParts(length, pieceIndex, offset), length);

    private IEnumerable<FilePart> GetParts(int length, int pieceIndex, int offset)
    {
        long byteOffset = pieceIndex * PieceSize + offset;
        int bytesRead = 0;
        int start = Search(byteOffset);
        for (int i = start; bytesRead <= length; i++)
        {
            FileData file = Files[i];
            long position = i == start ? byteOffset - file.ByteOffset : 0;
            int readLen = (int)long.Min(length - bytesRead, file.File.Length - position);
            yield return new(file, readLen, position);
            bytesRead += readLen;
        }
    }

    private int Search(long byteOffset)
    {
        int low = 0;
        int high = Files.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (mid < Files.Count - 1 && byteOffset >= Files[mid].ByteOffset && byteOffset < Files[mid + 1].ByteOffset)
            {
                return mid;
            }

            if (byteOffset < Files[mid].ByteOffset)
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
}
