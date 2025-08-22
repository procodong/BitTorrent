namespace BitTorrentClient.Application.Events.Listening;
public interface IEventListener
{
    Task ListenAsync(CancellationToken cancellationToken = default);
}