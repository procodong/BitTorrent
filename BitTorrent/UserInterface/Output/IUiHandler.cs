using BitTorrentClient.Models.Application;

namespace BitTorrentClient.UserInterface.Output;
public interface IUiHandler
{
    void Update(IEnumerable<DownloadUpdate> updates);
}
