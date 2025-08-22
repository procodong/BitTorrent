using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Helpers.DataStructures;
public interface IDisposeHandleProvider
{
    IAsyncDisposable GetDisposer();
}
