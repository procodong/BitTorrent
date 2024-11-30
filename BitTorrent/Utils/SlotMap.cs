using BitTorrent.Torrents.Peers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrent.Utils;
public readonly struct SlotMap<T> : IEnumerable<T>
{
    private readonly List<T?> _buffer;

    public SlotMap(int size)
    {
        _buffer = new List<T?>(size);
    }

    public SlotMap()
    {
        _buffer = [];
    }

    public int Count => _buffer.Count;

    public T this[int index]
    {
        get => _buffer[index] ?? throw new IndexOutOfRangeException();
    }

    public void Remove(int index)
    {
        _buffer[index] = default;
        int lastIndex = _buffer.Count - 1;
        int i = lastIndex - 1;
        while (i >= 0 && _buffer[i] is null) i--;
        _buffer.RemoveRange(i, lastIndex - i);
    }

    public int Add(T item) 
    {
        int index = _buffer.Count;
        _buffer.Add(item);
        return index;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _buffer.Where(v => v is not null).Select(v => v!).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_buffer).GetEnumerator();
    }
}
