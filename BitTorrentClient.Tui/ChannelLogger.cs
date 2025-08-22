using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Tui;

public class ChannelLogger : ILogger, IDisposable, IAsyncDisposable
{
    private readonly ChannelWriter<(LogLevel, string)> _messageWriter;
    private readonly StreamWriter _fullOutput;
    private readonly SemaphoreSlim _lock;

    public ChannelLogger(ChannelWriter<(LogLevel, string)> messageWriter, StreamWriter fullOutput)
    {
        _messageWriter = messageWriter;
        _fullOutput = fullOutput;
        _lock = new(1, 1);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public void Dispose()
    {
        _fullOutput.Dispose();
    }


    public ValueTask DisposeAsync()
    {
        return _fullOutput.DisposeAsync();
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (logLevel != LogLevel.Information)
        {

            _ = WriteToFileAsync(logLevel, message);
        }

        var sendTask = exception is null
            ? _messageWriter.WriteAsync((logLevel, message))
            : _messageWriter.WriteAsync((LogLevel.Error, exception.Message));
        if (!sendTask.IsCompleted)
        {
            _ = sendTask.AsTask();
        }
    }
    
    private async Task WriteToFileAsync(LogLevel level, string message)
    {
        await _lock.WaitAsync();
        try
        {
            await _fullOutput.WriteLineAsync($"[{DateTime.Now}] [{level}]: {message}");
        }
        catch (Exception ex)
        {
            await _messageWriter.WriteAsync((LogLevel.Error, $"Writing to log file {ex}"));
        }
        finally
        {
            _lock.Release();
        }
    }
}