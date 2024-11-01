using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public static class ListExt
{
    public static T SwapRemove<T>(this List<T> list, int index)
    {
        T old = list[index];   
        list[index] = list[^1];
        list.RemoveAt(list.Count - 1);
        return old;
    }
}
