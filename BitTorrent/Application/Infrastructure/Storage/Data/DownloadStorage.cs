using BitTorrentClient.Storage;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;
public class DownloadStorage(int pieceSize, List<StreamData> saves) : IDisposable, IAsyncDisposable
{
    private readonly int _pieceSize = pieceSize;
    private readonly List<StreamData> _saves = saves;

    public PieceStream GetStream(int pieceIndex, int offset, int length) => new(GetParts(length, pieceIndex, offset), length);

    private IEnumerable<StreamPart> GetParts(int length, int pieceIndex, int offset)
    {
        long byteOffset = pieceIndex * _pieceSize + offset;
        int bytesRead = 0;
        int start = Search(byteOffset);
        for (int i = start; bytesRead < length; i++)
        {
            StreamData file = _saves[i];
            long position = i == start ? byteOffset - file.ByteOffset : 0;
            int readLen = (int)long.Min(length - bytesRead, file.Size - position);
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
            if (fileData.Handle.IsValueCreated)
            {

                fileData.Handle.Value.Result.Dispose();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var fileData in _saves)
        {
            if (fileData.Handle.IsValueCreated)
            {

                await fileData.Handle.Value.Result.DisposeAsync();
            }
        }
    }
}
