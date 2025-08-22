using System.Diagnostics;

namespace BitTorrentClient.Helpers.DataStructures;
public class TransferTracker
{
    private readonly Stopwatch _uploadTimer;
    private long _transfer;

    public long TransferRate => (long)(_transfer / (double)_uploadTimer.Elapsed.TotalSeconds);

    public TransferTracker()
    {
        _uploadTimer = Stopwatch.StartNew();
    }

    public void RegisterTransfer(int transfer)
    {
        _transfer += transfer;
    }

    public void Reset()
    {
        _transfer = 0;
        _uploadTimer.Restart();
    }

    public int TimeUntilTransferRate(long rate)
    {
        return (int)((_transfer / rate) - _uploadTimer.Elapsed.TotalSeconds);
    }
}
