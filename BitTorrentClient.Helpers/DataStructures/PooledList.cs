using System.Buffers;

namespace BitTorrentClient.Helpers.DataStructures;

public class PooledList<T>
{
    private T[] _buffer;
    private int _expectedCapacity;
    private int _length;

    public PooledList(int size)
    {
        _expectedCapacity = int.Max(size, 8);
        _buffer = ArrayPool<T>.Shared.Rent(size);
    }

    public PooledList() : this(-1)
    {
        
    }
    
    public int Length => _length;

    public void Add(T item)
    {
        if (_length == _buffer.Length)
        {
            _expectedCapacity = _buffer.Length * 2;
            var newBuffer = ArrayPool<T>.Shared.Rent(_expectedCapacity);
            _buffer.AsSpan(0, _length).CopyTo(newBuffer);
            _buffer = newBuffer;
        }
        _buffer[_length++] = item;
    }

    public MaybeRentedArray<T> Take()
    {
        var old = new MaybeRentedArray<T>(_buffer, _length, rented: true);
        var expectedCapacity = _length < _expectedCapacity ? _expectedCapacity : _buffer.Length;
        _length = 0;
        _buffer = ArrayPool<T>.Shared.Rent(expectedCapacity);
        return old;
    }
}