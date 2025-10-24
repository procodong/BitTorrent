using System.Threading.Channels;

namespace BitTorrentClient.Helpers.DataStructures;

public class TaskListener<TIdentifier>
    where TIdentifier : struct, Enum
{
    private readonly Channel<Event<TIdentifier>> _taskChannel;
    private readonly CancellationToken _cancellationToken;

    public TaskListener(CancellationToken cancellationToken = default)
    {
        _taskChannel = Channel.CreateBounded<Event<TIdentifier>>(new BoundedChannelOptions(4)
        {
            SingleWriter = false
        });
        _cancellationToken = cancellationToken;
    }

    public void AddTask(TIdentifier identifier, Func<Task> factory)
    {
        _ = ListenAsync(identifier, factory);
    }

    public void AddTask(TIdentifier identifier, Task task)
    {
        _ = ListenAsync(identifier, task);
    }

    public ValueTask<Event<TIdentifier>> WaitAsync()
    {
        return _taskChannel.Reader.ReadAsync(_cancellationToken);
    }

    private async Task ListenAsync(TIdentifier identifier, Task task)
    {
        try
        {
            await task;
        }
        finally
        {
            await _taskChannel.Writer.WriteAsync(new(identifier, task), _cancellationToken);
        }
    }

    private async Task ListenAsync(TIdentifier identifier, Func<Task> taskFactory)
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            var task = taskFactory();
            try
            {
                await task;
            }
            finally
            {
                await _taskChannel.Writer.WriteAsync(new(identifier, task), _cancellationToken);
            }
        }
    }
}

public readonly struct Event<TIdentifier>
{
    private readonly Task _task;
    
    public TIdentifier EventType { get; }

    public T GetValue<T>() => ((Task<T>)_task).GetAwaiter().GetResult();

    public Event(TIdentifier eventType, Task task)
    {
        EventType = eventType;
        _task = task;
    }
    
    
}