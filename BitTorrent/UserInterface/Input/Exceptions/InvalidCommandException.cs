namespace BitTorrentClient.UserInterface.Input.Exceptions;
public class InvalidCommandException(string command) : Exception($"Invalid command {command}")
{
    public readonly string Command = command;
}
