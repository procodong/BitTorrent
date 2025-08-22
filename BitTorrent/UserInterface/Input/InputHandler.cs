using System.Threading.Channels;
using BitTorrentClient.Application.Events.Listening.Downloads;
using BitTorrentClient.UserInterface.Input.Parsing;

namespace BitTorrentClient.UserInterface.Input;
public class InputHandler
{
    private readonly ChannelWriter<Func<ICommandContext, Task>> _commandWriter;

    public InputHandler(ChannelWriter<Func<ICommandContext, Task>> commandWriter)
    {
        _commandWriter = commandWriter;
    }

    public async Task ListenAsync(TextReader input, TextWriter writer)
    {
        while (true)
        {
            var line = input.ReadLine();
            if (line is null) break;
            try
            {
                var command = CommandParser.ParseCommand(line);
                await _commandWriter.WriteAsync(command);
            }
            catch (Exception ex)
            {
                writer.WriteLine(ex.Message);
            }
        }
    }
}
