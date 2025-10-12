using System.Diagnostics;

namespace BitTorrentClient.Helpers.DataStructures;
public sealed class TransferTracker
{
    private readonly Stopwatch _uploadTimer;
    private long _transfer;

    public long TransferRate => (long)(_transfer / _uploadTimer.Elapsed.TotalSeconds);

    public TransferTracker()
    {
        _uploadTimer = Stopwatch.StartNew();
    }

    public void RegisterTransfer(long transfer)
    {
        _transfer += transfer;
    }

    public void Reset()
    {
        _transfer = 0;
        _uploadTimer.Restart();
    }

    public double SecondsUntilTransferRate(long rate)
    {
        return (double)_transfer / rate - _uploadTimer.Elapsed.TotalSeconds;
    }
}
