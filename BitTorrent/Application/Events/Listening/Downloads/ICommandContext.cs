namespace BitTorrentClient.Application.Events.Listening.Downloads;

public interface ICommandContext
{
    Task AddTorrentAsync(string torrentPath, string targetPath, string? name);
    Task RemoveTorrentAsync(string name);
    Task RemoveTorrentAsync(int index);
}
