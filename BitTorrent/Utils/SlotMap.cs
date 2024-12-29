using BitTorrentClient.Torrents.Peers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTorrentClient.Utils;
public class SlotMap<T> : IEnumerable<T>
    where T : class
{
    private readonly List<T?> _buffer;
    private int _offset;
    private int _clearedOffset;
    private int _count;

    public SlotMap(int size)
    {
        _buffer = new List<T?>(size);
    }

    public SlotMap()
    {
        _buffer = [];
    }

    public int Count => _count;

    public T this[int index]
    {
        get => _buffer[GetIndex(index)] ?? throw new IndexOutOfRangeException();
        set
        {
            int realIndex = GetIndex(index);
            if (_buffer[realIndex] is null) throw new IndexOutOfRangeException();
            _buffer[realIndex] = value;
        }
    }

    private int GetIndex(int index) => index - _offset;

    public void Remove(int index)
    {
        _buffer[GetIndex(index)] = default;
        _count--;
        while (_buffer.Count != 0 && _buffer[^1] is null)
        {
            _buffer.Pop();
        }
        while (_clearedOffset < _buffer.Count && _buffer[_clearedOffset] is null) _clearedOffset++;
        if (_clearedOffset >= _buffer.Count / 2)
        {
            _offset += _clearedOffset;
            _buffer.RemoveRange(0, _clearedOffset);
            _clearedOffset = 0;
        }
    }

    public int Add(T item) 
    {
        int index = _buffer.Count + _offset;
        _buffer.Add(item);
        _count++;
        return index;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return (IEnumerator<T>)_buffer.Where(v => v is not null).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _buffer.Where(v => v is not null).GetEnumerator();
    }
}
