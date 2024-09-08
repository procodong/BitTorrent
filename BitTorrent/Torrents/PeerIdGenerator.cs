using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents;
public class PeerIdGenerator
{
    private readonly Random _random = new();

    public string GeneratePeerId()
    {
        var number = _random.NextInt64(1_000_000_000_00, 9_999_999_999_99);
        return $"-BT0001-{number}";
    }
}
