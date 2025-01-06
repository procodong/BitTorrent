using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Storage;
public readonly record struct StreamData(long ByteOffset, long Size, Lazy<Task<StreamHandle>> Handle);
