using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Errors;
public class InvalidProtocolException(string protocol) : Exception($"Invalid peer protocol: {protocol}")
{
}
