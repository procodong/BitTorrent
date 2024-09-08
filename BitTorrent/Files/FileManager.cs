using BencodeNET.Torrents;

namespace BitTorrent.Files;
public class FileManager(int pieceSize)
{
    private readonly int PieceSize = pieceSize;
    private readonly List<FileData> Files = [];

    public void Create(string path, MultiFileInfoList files)
    {
        var createdDirectories = new HashSet<string>();
        ulong createdBytes = 0;
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
            Files.Add(new(createdFile, createdBytes));
            createdBytes += (ulong)file.FileSize;
        }
    }

    public async Task Read(Stream stream, int length, int pieceIndex, int offset)
    {
        await Operate(
            async (file, _, len) => await file.CopyToAsync(stream, len),
            length,
            pieceIndex,
            offset
            );
    }

    public async Task Write(ReadOnlyMemory<byte> buffer, int pieceIndex, int offset)
    {
        await Operate(
            async (file, offset, len) => await file.WriteAsync(buffer[offset..(offset + len)]),
            buffer.Length,
            pieceIndex,
            offset
            );
    }

    private async Task Operate(Func<FileStream, int, int, Task> op, int length, int pieceIndex, int offset)
    {
        ulong byteOffset = (ulong)pieceIndex * (ulong)PieceSize + (ulong)offset;
        int bytesRead = 0;
        int start = Search(byteOffset);
        for (int i = start; bytesRead <= length; i++)
        {
            var file = Files[i];
            file.File.Position = i == start ? (long)(byteOffset - file.ByteOffset) : 0;
            var readLen = (int)long.Min(length - bytesRead, file.File.Length - file.File.Position);
            await op(file.File, bytesRead, readLen);
            bytesRead += readLen;
        }
    }

    private int Search(ulong byteOffset)
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
