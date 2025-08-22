using System.Buffers;
using System.Runtime.CompilerServices;

namespace BitTorrentClient.Helpers.DataStructures;

public class PooledList<T>
{
    private T[] _buffer;
    private int _expectedCapacity;
    private int _length;

    public PooledList(int size)
    {
        _expectedCapacity = size;
        _buffer = ArrayPool<T>.Shared.Rent(size);
    }

    public PooledList() : this(8)
    {
        
    }
    
    public int Length => _length;

    public void Add(T item)
    {
        if (_length == _buffer.Length)
        {
            _expectedCapacity = _buffer.Length * 2;
            var newBuffer = ArrayPool<T>.Shared.Rent(_expectedCapacity);
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
            ArrayPool<T>.Shared.Return(_buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            _buffer = newBuffer;
        }
        _buffer[_length++] = item;
    }

    public Memory<T> Take()
    {
        var old = _buffer.AsMemory(0, _length);
        int expectedCapacity = _length < _expectedCapacity ? _expectedCapacity : _buffer.Length;
        _length = 0;
        _buffer = ArrayPool<T>.Shared.Rent(expectedCapacity);
        return old;
    }
}