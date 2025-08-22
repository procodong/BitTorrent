using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.UserInterface.Input.Parsing;

namespace BitTorrentClient.UserInterface.Input;
public class InputHandler
{
    private readonly ChannelWriter<Func<ICommandContext, Task>> _commandWriter;
    private readonly ChannelReader<byte[][]> _identifierTableReader;
    private byte[][] _identifierTable;

    public InputHandler(ChannelWriter<Func<ICommandContext, Task>> commandWriter, ChannelReader<byte[][]> identifierTableReader)
    {
        _commandWriter = commandWriter;
        _identifierTableReader = identifierTableReader;
        _identifierTable = [];
    }

    public async Task ListenAsync(TextReader input, TextWriter writer)
    {
        Task<byte[][]> identifierTableTask = _identifierTableReader.ReadAsync().AsTask();
        Task<string?> inputTask = input.ReadLineAsync();
        while (true)
        {
            var ready = await Task.WhenAny(identifierTableTask, inputTask);
            if (ready == inputTask)
            {
                try
                {
                    var line = await inputTask;
                    if (line is null) break;
                    var command = CommandParser.ParseCommand(line, _identifierTable);
                    await _commandWriter.WriteAsync(command);
                }
                catch (Exception ex)
                {
                    writer.WriteLine(ex.Message);
                }
                finally
                {
                    inputTask = input.ReadLineAsync();
                }
            }
            else if (ready == identifierTableTask)
            {
                var table = await identifierTableTask;
                _identifierTable = table;
                identifierTableTask = _identifierTableReader.ReadAsync().AsTask();
            }
        }
    }
}
