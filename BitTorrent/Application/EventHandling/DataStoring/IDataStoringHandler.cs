using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.EventHandling.DataStoring;
internal interface IDataStoringHandler
{
    Task SaveDataAsync(ReadOnlyMemory<byte> data);
}
