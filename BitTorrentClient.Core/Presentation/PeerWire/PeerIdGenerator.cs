using System.Text;

namespace BitTorrentClient.Core.Presentation.PeerWire;
public static class PeerIdGenerator
{
    public static string GeneratePeerId(string clientId, string clientVersion)
    {
        if (Encoding.ASCII.GetByteCount(clientId) != 2) throw new ArgumentException("Must be 2 ASCII characters", nameof(clientId));
        if (Encoding.ASCII.GetByteCount(clientVersion) != 4) throw new ArgumentException("Must be 4 ASCII characters", nameof(clientVersion));
        var number = Random.Shared.NextInt64(0, 1_000_000_000_000).ToString("D12");
        return $"-{clientId}{clientVersion}-{number}";
    }
}