namespace BitTorrentClient.Engine.Events.Listening.Interface;
public interface IEventListener
{
    Task ListenAsync(CancellationToken cancellationToken = default);
}