using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils;
public static class ListExt
{
    public static T SwapRemove<T>(this List<T> list, int index)
    {
        T old = list[index];
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
        T last = list[^1];
        list.RemoveAt(list.Count - 1);
        return last;
    } 
}
