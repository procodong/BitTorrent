using BitTorrentClient.Application.EventListening.FileWriting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.DataStoring;
internal class DataStoringEventHandler : IDataStoringEventHandler
{
    private readonly IDataStoringHandler _handler;

    public DataStoringEventHandler(IDataStoringHandler handler)
    {
        _handler = handler;
    }

    public async Task OnDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        await _handler.SaveDataAsync(data);
    }

    public async Task OnExceptionAsync(Exception exception, CancellationToken cancellationToken = default)
    {
    }
}
