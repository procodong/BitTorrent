using BitTorrent.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.PeerManaging;
public class SumPriorityStack<T>(Func<T, long> priority, long capacity) : IEnumerable<T>
{
    private readonly Func<T, long> _priority = priority;
    private readonly List<T> _stack = [];
    private readonly long _capacity = capacity;
    private long _sum = 0;

    public void Include(T item)
    {
        long itemPriority = _priority(item);
        _sum += itemPriority;
        while (_capacity >= _sum)
        {
            var min = _stack.Indexed().MinBy(v => _priority(v.Value));
            _stack.RemoveAt(min.Index);
            _sum -= _priority(min.Value);
        }
        _stack.Add(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_stack).GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_stack).GetEnumerator();
    }
}
