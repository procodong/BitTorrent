using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage;
public class StreamHandlePool : IDisposable, IAsyncDisposable
{
    private readonly Lazy<Task<StreamHandle[]>> _handles;

    public StreamHandlePool(int handleLimit, IStreamHandleFactory factory)
    {
        _handles = new Lazy<Task<StreamHandle[]>>(() =>
        {
            return Task.Run(() =>
            {
                var handles = new StreamHandle[handleLimit];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = factory.CreateStream();
                }
                return handles;
            });
        }, true);
    }

    public void Dispose()
    {
        if (_handles.IsValueCreated)
        {
            foreach (var handle in _handles.Value.Result)
            {
                handle.Stream.Dispose();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_handles.IsValueCreated)
        {
            foreach (var handle in _handles.Value.Result)
            {
                await handle.Stream.DisposeAsync();
            }
        }
    }

    public async Task<StreamHandle> GetHandle()
    {
        var handles = await _handles.Value;
        int index = Random.Shared.Next(0, handles.Length - 1);
        return handles[index];
    }
}
