namespace BitTorrentClient.Api.Information;

public readonly record struct ClientIdentifier((char, char) ClientId, ClientVersion ClientVersion);


public readonly record struct ClientVersion(char Major, char Minor, char Patch, char Revision)
{
    public override string ToString() => new([Major, Minor, Patch, Revision]);
}
