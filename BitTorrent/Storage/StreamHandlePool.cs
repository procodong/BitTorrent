using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Storage;
public class StreamHandlePool
{
    private readonly StreamHandle[] _handles;
    private readonly IStreamHandleFactory _factory;
    private volatile int _count;



    public StreamHandlePool(int handleLimit, IStreamHandleFactory factory)
    {
        _factory = factory;
        _handles = new StreamHandle[handleLimit];
    }

    public ValueTask<StreamHandle> GetHandle()
    {
        int index = Random.Shared.Next(0, _count - 1);
        var handle = _handles[index];
        throw new NotImplementedException();
    }
}
