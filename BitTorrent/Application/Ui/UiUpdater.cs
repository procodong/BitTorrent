using BitTorrent.Models.Application;
using System.Threading.Channels;

namespace BitTorrent.Application.Ui;
public class UiUpdater
{
    private readonly IUiHandler _uiHandler;

    public UiUpdater(IUiHandler uiHandler)
    {
        _uiHandler = uiHandler;
    }

    public async Task ListenAsync(ChannelReader<IEnumerable<DownloadUpdate>> updateReader)
    {
        await foreach (var updates in updateReader.ReadAllAsync())
        {
            _uiHandler.Update(updates);
        }
    }
}
