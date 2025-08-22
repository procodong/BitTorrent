namespace BitTorrentClient.Helpers.DataStructures;
internal class DisposeHandle : IAsyncDisposable
{
    private readonly IAsyncDisposable _disposable;
    public DisposeHandle(IAsyncDisposable disposable)
    {
        _disposable = disposable;
    }

    public ValueTask DisposeAsync()
    {
        return _disposable.DisposeAsync();
    }
}
