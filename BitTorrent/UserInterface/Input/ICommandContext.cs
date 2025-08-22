namespace BitTorrentClient.UserInterface.Input;
public interface ICommandContext
{
    Task AddTorrentAsync(string torrentPath, string targetPath);
    Task RemoveTorrentAsync(int index);
}
