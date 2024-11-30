using BitTorrent.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Torrents.Managing;
public class SumPriorityStack<T>(int capacity) : IEnumerable<(T Item, int Priority)>
{
    private readonly List<(T Item, int Priority)> _stack = [];
    private readonly int _capacity = capacity;
    private int _sum = 0;

    public void Include(T item, int priority, int addition)
    {
        _sum += addition;
        if (_sum >= _capacity)
        {
            _stack.Sort(Comparer<(T, int Priority)>.Create((v1, v2) => v1.Priority - v2.Priority));
        }
        while (_sum >= _capacity)
        {
            var (_, minPriority) = _stack.Pop();
            _sum -= minPriority;
        }
        _stack.Add((item, priority));
    }

    public long Sum => _sum;
    public long Capacity => _capacity;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _stack.GetEnumerator();
    }

    public IEnumerator<(T Item, int Priority)> GetEnumerator()
    {
        return _stack.GetEnumerator();
    }
}
