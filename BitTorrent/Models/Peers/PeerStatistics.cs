using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models.Peers;
public class PeerStatistics
{
    private long _downloaded = 0;
    private long _uploaded = 0;

    public void IncrementDownloaded(long download)
    {
        Interlocked.Add(ref _downloaded, download);
    }

    public long Downloaded
    {
        get => Interlocked.Read(ref _downloaded);
    }

    public void IncrementUploaded(long upload)
    {
        Interlocked.Add(ref _uploaded, upload);
    }

    public long Uploaded
    {
        get => Interlocked.Read(ref _uploaded);
    }
}