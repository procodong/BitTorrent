namespace BitTorrentClient.Helpers.Extensions;

public static class TaskExt
{
    public static async Task Catch(this Task task, Action<Exception> handler)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            handler(ex);
        }
    }

    public static async Task Catch<TState>(this Task task, TState state, Action<TState, Exception> handler)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            handler(state, ex);
        }
    }
}