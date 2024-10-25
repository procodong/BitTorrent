using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Files.DownloadFiles;
public readonly record struct FileData(FileStream File, long ByteOffset, SemaphoreSlim Lock);