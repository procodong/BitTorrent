namespace BitTorrentClient.Helpers.DataStructures;
public interface IDisposeHandleProvider<TDisposable>
where TDisposable : IAsyncDisposable
{
    TDisposable GetDisposer();
}
