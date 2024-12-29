using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils;
public class PriorityStack<T>(int size, IComparer<T> comparer) : IEnumerable<T>
{
    private readonly List<T> _stack = new(size);
    private readonly IComparer<T> _comparer = comparer;

    public void Include(T item)
    {
        if (_stack.Count == _stack.Capacity)
        {
            _stack.RemoveAt(_stack.Count - 1);
        }
        int index = _stack.TakeWhile(other => _comparer.Compare(other, item) > 0).Count();
        _stack.Insert(index, item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _stack.GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _stack.GetEnumerator();
    }
}
