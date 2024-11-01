using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Peers;
public class PeerStatistics
{
    public long Downloaded = 0;
    public long Uploaded = 0;

    public void IncrementDownloaded(long download)
    {
        Interlocked.Add(ref Downloaded, download);
    }

    public void IncrementUploaded(long upload)
    {
        Interlocked.Add(ref Uploaded, upload);
    }
}