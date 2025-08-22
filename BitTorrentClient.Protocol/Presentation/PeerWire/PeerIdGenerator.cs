using BitTorrentClient.Protocol.Presentation.PeerWire.Models;

namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public class PeerIdGenerator
{
    private readonly PeerIdentifier _id;

    public PeerIdGenerator(PeerIdentifier id)
    {
        _id = id;
    }

    public string GeneratePeerId()
    {
        var number = Random.Shared.NextInt64(1_000_000_000_00, 9_999_999_999_99);
        string id = $"-{_id.ClientId}{_id.Version}-{number}";
        if (id.Length != 20)
        {
            id = id[..20];
        }
        return id;
    }
}
