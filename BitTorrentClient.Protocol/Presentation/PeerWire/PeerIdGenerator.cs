namespace BitTorrentClient.Protocol.Presentation.PeerWire;
public class PeerIdGenerator
{
    private readonly string _clientId;
    private readonly string _clientVersion;

    public PeerIdGenerator(string clientId, string clientVersion)
    {
        _clientId = clientId;
        _clientVersion = clientVersion;
    }
    
    public string GeneratePeerId()
    {
        var number = Random.Shared.NextInt64(1_000_000_000_00, 9_999_999_999_99);
        string id = $"-{_clientId}{_clientVersion}-{number}";
        if (id.Length != 20)
        {
            id = id[..20];
        }
        return id;
    }
}