namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;
public sealed class StorageStream : IDisposable, IAsyncDisposable
{
    private readonly List<StreamData> _saves;
    private readonly long _size;

    public StorageStream(List<StreamData> saves, long size)
    {
        _saves = saves;
        _size = size;
    }

    public BlockStream GetStream(long offset, long length) => new(new(GetParts(offset, length)), length);

    public BlockStream GetStream(long offset) => GetStream(offset, _size - offset);

    private IEnumerable<StreamPart> GetParts(long offset, long length)
    {
        var bytesRead = 0;
        var start = Search(offset);
        for (var i = start; bytesRead < length; i++)
        {
            var file = _saves[i];
            var position = i == start ? offset - file.ByteOffset : 0;
            var readLen = (int)long.Min(length - bytesRead, file.Size - position);
            yield return new(file, readLen, position);
            bytesRead += readLen;
        }
    }

    private int Search(long byteOffset)
    {
        var low = 0;
        var high = _saves.Count - 1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
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
