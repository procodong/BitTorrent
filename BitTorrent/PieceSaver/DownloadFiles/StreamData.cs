using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Files.DownloadFiles;
public readonly record struct StreamData<S>(S Stream, long ByteOffset, SemaphoreSlim Lock) where S : Stream;