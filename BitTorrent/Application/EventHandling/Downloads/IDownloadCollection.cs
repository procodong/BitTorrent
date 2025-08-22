using BencodeNET.Torrents;
using BitTorrentClient.Application.Infrastructure.Storage.Data;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.EventHandling.Downloads;

public interface IDownloadCollection
{
    Task AddDownloadAsync(Torrent torrent, DownloadStorage storage, CancellationToken cancellationToken = default);
    Task RemoveDownloadAsync(int index);
    IEnumerable<DownloadUpdate> GetUpdates();
}