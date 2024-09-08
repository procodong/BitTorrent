using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Models;
public class PeerStatistics
{
    private ulong _downloaded = 0;
    private ulong _uploaded = 0;

    public void IncrementDownloaded(ulong download)
    {
        Interlocked.Add(ref _downloaded, download);
    }

    public ulong Downloaded
    {
        get => Interlocked.Read(ref _downloaded);
    }

    public void IncrementUploaded(ulong upload)
    {
        Interlocked.Add(ref _uploaded, upload);
    }

    public ulong Uploaded
    {
        get => Interlocked.Read(ref _uploaded);
    }
}