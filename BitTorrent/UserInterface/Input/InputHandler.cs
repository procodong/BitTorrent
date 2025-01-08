using BitTorrentClient.Application.Input.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Input;
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
