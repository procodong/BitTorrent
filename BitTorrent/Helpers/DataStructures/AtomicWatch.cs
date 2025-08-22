namespace BitTorrentClient.Helpers.DataStructures;
public class AtomicWatch
{

    private long _start;

    public AtomicWatch()
    {
        _start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _start, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    public long Elapsed => DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Interlocked.Read(ref _start);
}
