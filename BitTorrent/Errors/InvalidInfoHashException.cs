using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Errors;
public class InvalidInfoHashException(byte[] hash) : Exception
{
    public readonly byte[] Hash = hash;
}
