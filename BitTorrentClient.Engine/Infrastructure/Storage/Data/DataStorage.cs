using System.Collections.Concurrent;
using System.Threading.Channels;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Infrastructure.Storage.Data;

public class DataStorage : IDisposable, IAsyncDisposable
{
    private readonly StorageStream _stream;
    private readonly ChannelWriter<DownloadExecutionState> _downloadStateWriter;
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentStack<FailedWrite> _failedWrites;

    public DataStorage(StorageStream stream, ChannelWriter<DownloadExecutionState> downloadStateWriter, CancellationToken cancellationToken)
    {
        _stream = stream;
        _downloadStateWriter = downloadStateWriter;
        _cancellationToken = cancellationToken;
        _failedWrites = new();
    }

    public BlockStream GetData(long offset, int length)
    {
        return _stream.GetStream(offset, length);
    }

    public async Task WriteDataAsync(long offset, MaybeRentedArray<byte> array)
    {
        var stream = _stream.GetStream(offset);
        try
        {
            await stream.WriteAsync(array.Data, _cancellationToken);
        }
        catch (IOException)
        {
            _failedWrites.Push(new(offset, array));
            await _downloadStateWriter.WriteAsync(DownloadExecutionState.PausedAutomatically, _cancellationToken);
            return;
        }
        array.Dispose();
    }

    public void TryWritesAgain()
    {
        var writes = new FailedWrite[_failedWrites.Count];
        int count = _failedWrites.TryPopRange(writes);
        for (int i = 0; i < count; i++)
        {
            ref var write = ref writes[i];
            _ = WriteDataAsync(write.Offset, write.Array);
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _stream.DisposeAsync();
    }
}

readonly record struct FailedWrite(long Offset, MaybeRentedArray<byte> Array);