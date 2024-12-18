using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Storage;
public class StreamHandlePool
{
    private readonly List<StreamHandle> _handles;
    private readonly IStreamHandleFactory _factory;

    public StreamHandlePool(int handleLimit, IStreamHandleFactory factory)
    {
        _factory = factory;
        _handles = new(handleLimit);
    }

    public ValueTask<StreamHandle> GetHandle()
    {
        foreach (var handle in _handles)
        {
            if (handle.Lock.CurrentCount != 0)
            {
                return ValueTask.FromResult(handle);
            }
        }
        if (_handles.Count != _handles.Capacity)
        {
            return new(Task.Run(_factory.CreateStream));
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
