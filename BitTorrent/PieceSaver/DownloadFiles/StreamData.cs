using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Files.DownloadFiles;
public readonly record struct StreamData(Stream Stream, long ByteOffset, SemaphoreSlim Lock);