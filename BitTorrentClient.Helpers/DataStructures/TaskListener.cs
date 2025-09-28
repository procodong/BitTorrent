using System.Threading.Channels;

namespace BitTorrentClient.Helpers.DataStructures;

public class TaskListener<TIdentifier>
    where TIdentifier : struct, Enum
{
    private readonly Channel<(TaskFactory<TIdentifier>, Task)> _taskChannel;
    private readonly CancellationToken _cancellationToken;

    public TaskListener(CancellationToken cancellationToken = default)
    {
        _taskChannel = Channel.CreateBounded<(TaskFactory<TIdentifier>, Task)>(new BoundedChannelOptions(4)
        {
            SingleWriter = false
        });
        _cancellationToken = cancellationToken;
    }

    public void AddTask(TIdentifier identifier, Func<Task> factory)
    {
        _ = ListenAsync(factory(), new(factory, identifier));
    }

    public void AddTask(TIdentifier identifier, Task task)
    {
        _ = ListenAsync(task, new(null, identifier));
    }

    public async Task<(TIdentifier, Task)> WaitAsync()
    {
        var (factory, task) = await _taskChannel.Reader.ReadAsync();
        if (factory.Factory is not null)
        {
            _ = ListenAsync(factory.Factory(), factory);
        }
        return (factory.Identifier, task);
    }

    private async Task ListenAsync(Task task, TaskFactory<TIdentifier> taskFactory)
    {
        try
        {
            await task;
        }
        finally
        {
            await _taskChannel.Writer.WriteAsync((taskFactory, task), _cancellationToken);
        }
    }
}

readonly record struct TaskFactory<T>(Func<Task>? Factory, T Identifier);