using BitTorrent.Application.Input.Commands;
using BitTorrent.Application.Input.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Input.Parsing;
public static class CommandParser
{
    public static ICommand ParseCommand(string line)
    {
        var parser = new Parser(line);
        var command = parser.ParseIdentifier();
        if (command.SequenceEqual("download"))
        {
            return new CreateTorrentCommand(parser.ParseString().ToString(), parser.ParseString().ToString());
        }
        throw new InvalidCommandException(parser.ParseIdentifier().ToString());
    }
}
