using System.Threading.Channels;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.UserInterface.Output;
public class UiUpdater
{
    private readonly UiHandler _uiHandler;

    public UiUpdater(UiHandler uiHandler)
    {
        _uiHandler = uiHandler;
    }

    public async Task ListenAsync(ChannelReader<IEnumerable<DownloadUpdate>> updateReader)
    {
        await foreach (var updates in updateReader.ReadAllAsync())
        {
            await _uiHandler.UpdateAsync(updates);
        }
    }
}
