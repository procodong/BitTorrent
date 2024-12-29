using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage;
public readonly record struct StreamHandle(SemaphoreSlim Lock, Stream Stream);