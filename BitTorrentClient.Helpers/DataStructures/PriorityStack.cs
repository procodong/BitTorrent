using System.Collections;

namespace BitTorrentClient.Helpers.DataStructures;
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
        var index = _stack.TakeWhile(other => _comparer.Compare(other, item) > 0).Count();
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
