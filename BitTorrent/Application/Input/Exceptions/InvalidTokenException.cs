using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Application.Input.Exceptions;
internal class InvalidTokenException(char token, int index) : Exception($"Invalid token {token} at {index}")
{
    public readonly char Token = token;
    public readonly int Index = index;
}
