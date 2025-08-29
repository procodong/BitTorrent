namespace BitTorrentClient.Helpers.Extensions;
public static class ListExt
{
    public static T SwapRemove<T>(this List<T> list, int index)
    {
        var old = list[index];
        if (list.Count == 1)
        {
            list.Clear();
            return old;
        }
        list[index] = list.Pop();
        return old;
    }

    public static T Pop<T>(this List<T> list)
    {
        var last = list[^1];
        list.RemoveAt(list.Count - 1);
        return last;
    } 
}
