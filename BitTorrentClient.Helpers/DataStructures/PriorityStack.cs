using System.Collections;

namespace BitTorrentClient.Helpers.DataStructures;
public sealed class PriorityStack<T> : IEnumerable<T>
{
    private readonly List<T> _stack;
    private readonly IComparer<T> _comparer;

    public PriorityStack(int size, IComparer<T> comparer)
    {
        _stack = new(size);
        _comparer = comparer;
    }

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
