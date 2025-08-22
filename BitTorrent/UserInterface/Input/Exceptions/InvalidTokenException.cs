namespace BitTorrentClient.UserInterface.Input.Exceptions;
internal class InvalidTokenException(char token, int index) : Exception($"Invalid token {token} at {index}")
{
    public readonly char Token = token;
    public readonly int Index = index;
}
