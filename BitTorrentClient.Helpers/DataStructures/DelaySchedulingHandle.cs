namespace BitTorrentClient.Helpers.DataStructures;

public sealed class DelaySchedulingHandle
{
    private readonly Action<TimeSpan> _scheduler;

    public DelaySchedulingHandle(Action<TimeSpan> scheduler)
    {
        _scheduler = scheduler;
    }

    public void Schedule(TimeSpan milliseconds)
    {
        _scheduler(milliseconds);
    }
}