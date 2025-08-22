using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Helpers;
public class MemoryComparer<T> : IEqualityComparer<ReadOnlyMemory<T>>
{
    public bool Equals(ReadOnlyMemory<T> x, ReadOnlyMemory<T> y)
    {
        return x.Span.SequenceEqual(y.Span);
    }

    public int GetHashCode(ReadOnlyMemory<T> obj)
    {
        var hash = new HashCode();
        foreach (var b in obj.Span)
        {
            hash.Add(b);
        }
        return hash.ToHashCode();
    }
}