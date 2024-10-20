using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Downloads;
public class PieceDownloadSlot(FileStream fileBuffer, SemaphoreSlim fileLock, ReaderWriterLock downloadLock, PieceDownload? download)
{
    public readonly FileStream FileBuffer = fileBuffer;
    public readonly SemaphoreSlim FileLock = fileLock;
    public PieceDownload? Download = download;
    public readonly ReaderWriterLock Lock = downloadLock;
}
