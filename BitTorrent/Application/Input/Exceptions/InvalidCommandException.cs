using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Application.Input.Exceptions;
public class InvalidCommandException(string command) : Exception($"Invalid command {command}")
{
    public readonly string Command = command;
}
