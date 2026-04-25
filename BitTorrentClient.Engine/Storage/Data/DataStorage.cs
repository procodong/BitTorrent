using System.Collections.Concurrent;
using System.Threading.Channels;
using BitTorrentClient.Engine.Models.Downloads;
using BitTorrentClient.Helpers.DataStructures;

namespace BitTorrentClient.Engine.Storage.Data;

public sealed class DataStorage : IDisposable, IAsyncDisposable
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
        try
        {
            var stream = _stream.GetStream(offset);
            await stream.WriteAsync(array.Data, _cancellationToken);
        }
        catch (IOException)
        {
            bool success;
            do
            {
                var callback = new TaskCompletionSource<bool>();
                _failedWrites.Push(new(offset, array, callback));
                await _downloadStateWriter.WriteAsync(DownloadExecutionState.PausedAutomatically, _cancellationToken);
                success = await callback.Task;
            } while (!success);
        }
    }

    public void TryWritesAgain()
    {
        var writes = new FailedWrite[_failedWrites.Count];
        var count = _failedWrites.TryPopRange(writes);
        for (var i = 0; i < count; i++)
        {
            var write = writes[i];
            _ = _stream.GetStream(write.Offset).WriteAsync(write.Array.Data, _cancellationToken).AsTask().ContinueWith(t => write.Callback.SetResult(t.IsCompletedSuccessfully), _cancellationToken);
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

readonly record struct FailedWrite(long Offset, MaybeRentedArray<byte> Array, TaskCompletionSource<bool> Callback);