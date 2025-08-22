using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Helpers;

public class ChannelLogger : ILogger, IDisposable, IAsyncDisposable
{
    private readonly ChannelWriter<(LogLevel, string)> _messageWriter;
    private readonly StreamWriter _fullOutput;

    public ChannelLogger(ChannelWriter<(LogLevel, string)> messageWriter, StreamWriter fullOutput)
    {
        _messageWriter = messageWriter;
        _fullOutput = fullOutput;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public void Dispose()
    {
        ((IDisposable)_fullOutput).Dispose();
    }


    public ValueTask DisposeAsync()
    {
        return ((IAsyncDisposable)_fullOutput).DisposeAsync();
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        if (logLevel != LogLevel.Information)
        {

            _ = WriteToFileAsync(logLevel, message);
        }
        ValueTask sendTask;
        if (exception is not null)
        {
            sendTask = _messageWriter.WriteAsync((LogLevel.Error, exception.Message));
        }
        else
        {
            sendTask = _messageWriter.WriteAsync((logLevel, message));
        }
        if (!sendTask.IsCompleted)
        {
            sendTask.AsTask().GetAwaiter().GetResult();
        }
    }

    private async Task WriteToFileAsync(LogLevel level, string message)
    {
        try
        {
            await _fullOutput.WriteLineAsync($"[{DateTime.Now}] [{level}]: {message}");
        }
        catch (Exception ex)
        {
            await _messageWriter.WriteAsync((LogLevel.Error, $"Writing to log file {ex}"));
        }
    }
}