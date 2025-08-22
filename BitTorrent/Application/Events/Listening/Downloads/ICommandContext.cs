namespace BitTorrentClient.Application.Events.Listening.Downloads;
public interface ICommandContext
{
    Task AddTorrentAsync(string torrentPath, string targetPath);
    Task RemoveTorrentAsync(ReadOnlyMemory<byte> index);
}
