using System.Collections;

namespace BitTorrentClient.Helpers.DataStructures;
public sealed class SlotMap<T> : IEnumerable<T>
{
    private readonly List<Slot<T>> _buffer;
    private readonly Stack<int> _freePlaces;
    private int _count;

    public SlotMap()
    {
        _buffer = [];
        _freePlaces = [];
    }

    public int Count => _count;

    public T this[int index]
    {
        get => _buffer[index].Value;
    }


    public void Remove(int index)
    {
        _freePlaces.Push(index);
        _buffer[index] = new();
        _count--;
    }

    public int Add(T item)
    {
        if (_freePlaces.TryPop(out var place))
        {
            _buffer[place] = new(item);
            return place;
        }

        _buffer.Add(new(item));
        return _buffer.Count - 1;
    }

    public T Add(Func<int, T> func)
    {
        if (_freePlaces.TryPop(out var place))
        {
            var item = func(place);
            _buffer[place] = new(item);
            return item;
        }

        var value = func(_buffer.Count);
        _buffer.Add(new(value));
        return value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _buffer.Where(v => v.IsSet).Select(v => v.Value).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _buffer.Where(v => v.IsSet).GetEnumerator();
    }
}

record struct Slot<T>(T Value, bool IsSet = true);