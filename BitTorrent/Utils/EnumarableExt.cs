using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public static class EnumarableExt
{
    public static IEnumerable<(int Index, T Value)> Indexed<T>(this IEnumerable<T> source)
    {
        return source.Select((v, i) => (i, v));
    }

    public static T? Find<T>(this IEnumerable<T> source, Func<T, bool> condition)
    {
        foreach (var item in source)
        {
            if (condition(item)) return item;
        }
        return default;
    } 
}
