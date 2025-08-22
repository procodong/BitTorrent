using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventListening.FileWriting;
internal interface IDataStoringEventHandler
{
    Task OnDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    Task OnExceptionAsync(Exception exception, CancellationToken cancellationToken = default);
}
