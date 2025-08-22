namespace BitTorrentClient.Application.EventListening.Downloads;
public interface ICommandContext
{
    Task AddTorrentAsync(string torrentPath, string targetPath);
    Task RemoveTorrentAsync(ReadOnlyMemory<byte> index);
}
