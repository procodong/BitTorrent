namespace BitTorrentClient.Protocol.Presentation.PeerWire.Models;

public readonly record struct PeerIdentifier(string ClientId, Version Version);


public readonly record struct Version(char Major, char Minor, char Patch, char Revision)
{
    public override string ToString() => string.Create(4, this, (span, ver) =>
    {
        span[0] = ver.Major;
        span[1] = ver.Minor;
        span[2] = ver.Patch;
        span[3] = ver.Revision;
    });
}
