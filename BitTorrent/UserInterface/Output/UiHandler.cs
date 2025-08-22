using System.Text;
using System.Threading.Channels;
using BitTorrentClient.Helpers.Extensions;
using BitTorrentClient.Models.Application;

namespace BitTorrentClient.UserInterface.Output;
public class UiHandler
{
    private readonly UiDrawer _drawer;
    private readonly ChannelWriter<byte[][]> _identifierTableWriter;
    private readonly List<byte[]> _identifiers;

    public UiHandler(UiDrawer drawer, ChannelWriter<byte[][]> identifierTableWriter)
    {
        _drawer = drawer;
        _identifierTableWriter = identifierTableWriter;
        _identifiers = [];
    }

    public async Task UpdateAsync(IEnumerable<DownloadUpdate> updates)
    {
        _drawer.StartDraw();
        foreach (var (index, update) in updates.Indexed())
        {
            if (index >= _identifiers.Count)
            {
                _identifiers.Add(update.Identifier);
                await _identifierTableWriter.WriteAsync(_identifiers.ToArray());
            }
            else
            {
                _identifiers[index] = update.Identifier;
            }
            _drawer.Draw(index, update);
        }
        _drawer.EndDraw();
    }
}
