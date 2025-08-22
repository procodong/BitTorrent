using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public class PeerIdGenerator
{
    private readonly Random _random;
    private readonly string _clientId;
    private readonly int _version;

    public PeerIdGenerator(string clientId, int version)
    {
        _clientId = clientId;
        _version = version;
        _random = new();
    }

    public string GeneratePeerId()
    {
        var number = _random.NextInt64(1_000_000_000_00, 9_999_999_999_99);
        string id = $"-{_clientId}{_version}-{number}";
        if (id.Length != 20)
        {
            id = id[..20];
        }
        return id;
    }
}
