using BitTorrentClient.UserInterface.Input;

namespace BitTorrentClient.Application.EventListening.Downloads;

public interface IDownloadEventHandler : ICommandContext
{
    Task OnTickAsync(CancellationToken cancellationToken = default);
}