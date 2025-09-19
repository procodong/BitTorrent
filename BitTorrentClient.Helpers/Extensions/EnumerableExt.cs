namespace BitTorrentClient.Helpers.Extensions;
public static class EnumerableExt
{
    public static T? Find<T>(this IEnumerable<T> source, Func<T, bool> condition)
        where T : struct
    {
        foreach (var item in source)
        {
            if (condition(item)) return item;
        }
        return default;
    } 
}
