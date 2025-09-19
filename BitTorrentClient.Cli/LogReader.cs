using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace BitTorrentClient.Cli;

public sealed class LogReader
{
    private volatile string _latestMessage = string.Empty;

    public string LatestMessage => _latestMessage;

    public async Task ReadLogs(ChannelReader<(LogLevel, string)> reader)
    {
        await foreach (var (_, message) in reader.ReadAllAsync())
        {
            _latestMessage = message;
        }
    }
}