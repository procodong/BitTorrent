using BitTorrentClient.Models.Application;

namespace BitTorrentClient.Application.Infrastructure.Downloads;

public interface IApplicationUpdateProvider
{
    DownloadUpdate GetUpdate();
}