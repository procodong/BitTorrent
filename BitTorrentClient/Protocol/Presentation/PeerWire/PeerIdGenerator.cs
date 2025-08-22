using BitTorrentClient.Models.Peers;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public class PeerIdGenerator
{
    private readonly Random _random;
    private readonly PeerIdentifier _id;

    public PeerIdGenerator(PeerIdentifier id)
    {
        _id = id;
        _random = new();
    }

    public string GeneratePeerId()
    {
        var number = _random.NextInt64(1_000_000_000_00, 9_999_999_999_99);
        string id = $"-{_id.ClientId}{_id.Version}-{number}";
        if (id.Length != 20)
        {
            id = id[..20];
        }
        return id;
    }
}
