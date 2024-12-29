using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Models.Peers;
public class DataTransferCounter
{
    public long Downloaded = 0;
    public long Uploaded = 0;

    public DataTransferVector AsVector() => new(Downloaded, Uploaded);

    public DataTransferVector FetchReplace(DataTransferVector vector)
    {
        long downloaded = Interlocked.Exchange(ref Downloaded, vector.Download);
        long uploaded = Interlocked.Exchange(ref Uploaded, vector.Upload);
        return new(downloaded, uploaded);
    }

    public DataTransferVector Fetch()
    {
        return new(
            Interlocked.Read(ref Downloaded),
            Interlocked.Read(ref Uploaded)
            );
    }

    public void AtomicAdd(DataTransferVector vector)
    {
        Interlocked.Add(ref Downloaded, vector.Download);
        Interlocked.Add(ref Uploaded, vector.Upload);
    }
}
