namespace BitTorrentClient.Api.Information;

public readonly record struct PeerIdentifier((char, char) ClientId, ClientVersion ClientVersion);


public readonly record struct ClientVersion(char Major, char Minor, char Patch, char Revision)
{
    public override string ToString() => string.Create(4, this, (span, ver) =>
    {
        span[0] = ver.Major;
        span[1] = ver.Minor;
        span[2] = ver.Patch;
        span[3] = ver.Revision;
    });
}
