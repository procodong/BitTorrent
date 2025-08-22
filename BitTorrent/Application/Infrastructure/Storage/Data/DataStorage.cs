using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Storage.Data;

public class DataStorage
{
    private readonly FileStreamProvider _streamProvider;
    private readonly ChannelWriter<DownloadExecutionState> _downloadStateWriter;
    private readonly CancellationToken _cancellationToken;
    private readonly ConcurrentStack<FailedWrite> _failedWrites;

    public DataStorage(FileStreamProvider streamProvider, ChannelWriter<DownloadExecutionState> downloadStateWriter, CancellationToken cancellationToken)
    {
        _streamProvider = streamProvider;
        _downloadStateWriter = downloadStateWriter;
        _cancellationToken = cancellationToken;
        _failedWrites = new();
    }

    public BlockStream GetData(long offset, int length)
    {
        return _streamProvider.GetStream(offset, length);
    }

    public async Task WriteDataAsync(long offset, ArraySegment<byte> data, bool rented = false)
    {
        var stream = _streamProvider.GetStream(offset);
        try
        {
            await stream.WriteAsync(data, _cancellationToken);
        }
        catch (IOException)
        {
            _failedWrites.Push(new(offset, data, rented));
            await _downloadStateWriter.WriteAsync(DownloadExecutionState.PausedAutomatically, _cancellationToken);
            return;
        }

        if (rented && data.Array is not null)
        {
            ArrayPool<byte>.Shared.Return(data.Array);
        }
    }

    public void TryWritesAgain()
    {
        var writes = new FailedWrite[_failedWrites.Count];
        int count = _failedWrites.TryPopRange(writes);
        for (int i = 0; i < count; i++)
        {
            ref var write = ref writes[i];
            _ = WriteDataAsync(write.Offset, write.Data, write.Rented);
        }
    }
}

readonly record struct FailedWrite(long Offset, ArraySegment<byte> Data, bool Rented);