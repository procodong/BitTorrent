using BitTorrentClient.Application.EventListening.Downloads;
using BitTorrentClient.UserInterface.Input.Exceptions;

namespace BitTorrentClient.UserInterface.Input.Parsing;
public static class CommandParser
{
    public static Func<ICommandContext, Task> ParseCommand(string line)
    {
        var parser = new Parser(line);
        var command = parser.ParseIdentifier();
        if (command.SequenceEqual("download"))
        {
            var torrentPath = parser.ParseString().ToString();
            var path = parser.ParseString().ToString();
            return (ctx) => ctx.AddTorrentAsync(torrentPath, path);
        }
        else if (command.SequenceEqual("remove"))
        {
            int index = parser.ParseInteger();
            return (ctx) => ctx.RemoveTorrentAsync(index);
        }
        throw new InvalidCommandException(command.ToString());
    }
}
